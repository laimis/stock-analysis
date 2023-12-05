using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Stocks
{
    public class PositionEventType
    {
        public const string Buy = "buy";
        public const string Stop = "stop";
        public const string Sell = "sell";
        public const string Risk = "risk";

        public PositionEventType(string value)
        {
            Value = value switch
            {
                Buy => Buy,
                Stop => Stop,
                Sell => Sell,
                Risk => Risk,
                _ => throw new InvalidOperationException($"Invalid position event type: {value}")
            };
        }

        public string Value
        {
            get; 
        }
    }
    
    public readonly record struct PositionEvent (Guid Id, string Description, PositionEventType Type, decimal? Value, DateOnly When, decimal? Quantity = null)
    {
        public readonly string Date => When.ToString("yyyy-MM-dd");
    }
    public readonly record struct PositionTransaction(decimal NumberOfShares, decimal Price, Guid TransactionId, string Type, DateTimeOffset When)
    {
        public readonly string Date => When.ToString("yyyy-MM-dd");
        public readonly int AgeInDays => (int)(DateTimeOffset.UtcNow - When).TotalDays;
    }

    // PositionInstance models a stock position from the time the first share is opened
    // to the time when the last share is sold.
    public class PositionInstance
    {
        public PositionInstance(int positionId, Ticker ticker, DateTimeOffset opened)
        {
            PositionId = positionId;
            Ticker = ticker;
            Opened = opened;
        }

        public decimal NumberOfShares { get; private set; }
        public decimal AverageCostPerShare { get; private set; }
        public decimal AverageSaleCostPerShare { get; private set; }
        public decimal AverageBuyCostPerShare { get; private set; }
        public DateTimeOffset Opened { get; }
        public int DaysHeld => (int)(Closed ?? DateTimeOffset.UtcNow).Subtract(Opened).TotalDays;
        public decimal Cost { get; private set; }
        public decimal Profit { get; private set; }
        public decimal GainPct => IsClosed switch {
            true => (AverageSaleCostPerShare - AverageBuyCostPerShare) / AverageBuyCostPerShare,
            false => 0
        };
            
        public decimal RR => RiskedAmount switch {
            not null => 
                RiskedAmount.Value switch
                {
                    0 => 0,
                    _ => Profit / RiskedAmount.Value
                },
            _ => 0
        };
        public decimal RRWeighted => RR * Cost;

        public bool IsClosed => Closed != null;
        public int PositionId { get; }
        public Ticker Ticker { get; }
        public DateTimeOffset? Closed { get; private set; }
        public decimal? FirstStop { get; private set; }
        public decimal? RiskedAmount { get; private set; }
        public decimal? CostAtRiskedBasedOnStopPrice => StopPrice switch {
            not null => StopPrice > AverageCostPerShare ? 0 : (AverageCostPerShare - StopPrice) * NumberOfShares,
            _ => null
        };
        
        public List<PositionTransaction> Transactions { get; } = new();
        public List<PositionEvent> Events { get; } = new();

        public decimal? StopPrice { get; private set; }
        public DateTimeOffset LastTransaction { get; private set; }
        public decimal LastSellPrice { get; private set; }
        public int DaysSinceLastTransaction => (int)(DateTimeOffset.UtcNow - LastTransaction).TotalDays;
        private readonly List<decimal> _slots = new();

        private bool _positionCompleted;
        public decimal CompletedPositionCost { get; private set; }
        public decimal CompletedPositionShares { get; private set; }
        public decimal CompletedPositionCostPerShare => CompletedPositionCost / CompletedPositionShares;

        public TradeGrade? Grade { get; private set; }
        public string GradeNote { get; private set; }
        public void SetGrade(TradeGrade grade, string note = null)
        {
            Grade = grade;
            var previousNote = GradeNote;
            GradeNote = note;
            Notes.Remove(previousNote);
            Notes.Add(note);
        }

        public List<string> Notes { get; } = new();
        public void AddNotes(string notes)
        {
            Notes.Add(notes);
        }

        public void Buy(decimal numberOfShares, decimal price, DateTimeOffset when, Guid transactionId, string notes = null)
        {
            Transactions.Add(new PositionTransaction(numberOfShares, price, TransactionId: transactionId, Type:"buy", when));
            Events.Add(new PositionEvent(transactionId, $"buy {numberOfShares} @ ${price}", Type: new PositionEventType(PositionEventType.Buy), Value: price, When: DateOnly.FromDateTime(when.DateTime), Quantity: numberOfShares));

            if (_positionCompleted == false)
            {
                CompletedPositionCost += price * numberOfShares;
                CompletedPositionShares += numberOfShares;
            }

            if (notes != null)
            {
                Notes.Add(notes);
            }

            LastTransaction = when;

            RunCalculations();
        }

        internal void RemoveTransaction(Guid transactionId)
        {
            var tx = Transactions.SingleOrDefault(t => t.TransactionId == transactionId);
            if (tx.TransactionId == default)
            {
                throw new InvalidOperationException($"Transaction {transactionId} not found");
            }
            Transactions.Remove(tx);

            var ev = Events.SingleOrDefault(e => e.Id == transactionId);
            if (ev.Id == default)
            {
                throw new InvalidOperationException($"Event {transactionId} not found");
            }
            Events.Remove(ev);
            
            RunCalculations();
        }

        public void Sell(decimal numberOfShares, decimal price, Guid transactionId, DateTimeOffset when, string notes = null)
        {
            if (NumberOfShares - numberOfShares < 0)
            {
                var details = $"Sell {numberOfShares} @ ${price} on {when} for {Ticker}";
                throw new InvalidOperationException("Transaction would make amount owned invalid: " + details);
            }

            // once we stop adding and do our first sale, that should be our position
            // completion
            if (!_positionCompleted)
            {
                _positionCompleted = true;
            }

            var percentGainAtSale = (price - AverageCostPerShare) / AverageCostPerShare;

            Transactions.Add(new PositionTransaction(numberOfShares, price, TransactionId:transactionId, Type: "sell", when));
            Events.Add(new PositionEvent(transactionId, $"sell {numberOfShares} @ ${price} ({percentGainAtSale:P})", new PositionEventType(PositionEventType.Sell), price, DateOnly.FromDateTime(when.DateTime), Quantity: numberOfShares));

            // if we haven't set the risked amount, when we set it at 5% from the first buy price?
            if (StopPrice == null && !HasEventWithDescription("Stop price deleted"))
            {
                SetStopPrice(CompletedPositionCostPerShare * 0.95m, when);
            }            

            if (notes != null)
            {
                Notes.Add(notes);
            }

            LastTransaction = when;
            LastSellPrice = price;

            RunCalculations();

            if (NumberOfShares == 0)
            {
                Closed = when;
            }
        }

        private bool HasEventWithDescription(string testDescription) => Events.Any(e => e.Description == testDescription);

        public void SetStopPrice(decimal? stopPrice, DateTimeOffset when)
        {
            if (stopPrice != null && stopPrice != StopPrice)
            {
                StopPrice = stopPrice;

                FirstStop ??= stopPrice;

                var stopPercentage = (stopPrice.Value - AverageCostPerShare) / AverageCostPerShare;

                Events.Add(
                    new PositionEvent(Guid.Empty, $"Stop price set to {stopPrice.Value:0.##} ({stopPercentage:P1})", new PositionEventType(PositionEventType.Stop), stopPrice, DateOnly.FromDateTime(when.DateTime)));

                if (RiskedAmount == null)
                {
                    SetRiskAmount((AverageCostPerShare - stopPrice.Value) * NumberOfShares, when);
                }
            }
        }

        public void DeleteStopPrice(DateTimeOffset when)
        {
            StopPrice = null;
            RiskedAmount = null;
            
            Events.Add(new PositionEvent(Guid.Empty, "Stop price deleted", new PositionEventType(PositionEventType.Stop), null, DateOnly.FromDateTime(when.DateTime)));
        }

        public void SetRiskAmount(decimal riskAmount, DateTimeOffset when)
        {
            if (riskAmount == 0)
            {
                return;
            }
            
            RiskedAmount = riskAmount;

            Events.Add(new PositionEvent(Guid.Empty, $"Set risk amount to {RiskedAmount.Value:0.##}", new PositionEventType(PositionEventType.Risk), riskAmount, DateOnly.FromDateTime(when.DateTime)));
        }

        private void RunCalculations()
        {
            _slots.Clear();
            decimal cost = 0;
            decimal profit = 0;
            decimal numberOfShares = 0;
            
            decimal totalSale = 0;
            decimal totalNumberOfSharesSold = 0;

            decimal totalBuy = 0;
            decimal totalNumberOfSharesBought = 0;

            Transactions.ForEach(transaction => {
            if (transaction.Type == "buy")
            {
                for (var i = 0; i < transaction.NumberOfShares; i++)
                {
                    _slots.Add(transaction.Price);
                    cost += transaction.Price;
                    numberOfShares++;
                }

                totalBuy += transaction.Price * transaction.NumberOfShares;
                totalNumberOfSharesBought += transaction.NumberOfShares;
            }
            else
            {
                // remove quantity number of slots from the beginning of an array
                var removed = _slots.Take((int)transaction.NumberOfShares).ToList();
                _slots.RemoveRange(0, (int)transaction.NumberOfShares);
                removed.ForEach(
                    removedElement =>
                    {
                        profit += transaction.Price - removedElement;
                        cost -= removedElement;
                        numberOfShares--;
                    }
                );
                
                totalSale += transaction.Price * transaction.NumberOfShares;
                totalNumberOfSharesSold += transaction.NumberOfShares;
            }
            });

            // calculate average cost per share using slots
            AverageCostPerShare = _slots.Count switch {
                0 => 0,
                _ => _slots.Sum() / _slots.Count
            };
            Cost = cost;
            Profit = profit;
            NumberOfShares = numberOfShares;

            AverageSaleCostPerShare = totalNumberOfSharesSold switch {
                0 => 0,
                _ => totalSale / totalNumberOfSharesSold
            };

            AverageBuyCostPerShare = totalNumberOfSharesBought switch {
                0 => 0,
                _ => totalBuy / totalNumberOfSharesBought
            };
        }

        private readonly Dictionary<string, string> _labels = new();
        public IEnumerable<KeyValuePair<string, string>> Labels => _labels;
        public bool ContainsLabel(string key, string value)
        {
            if (!ContainsLabel(key))
            {
                return false;
            }

            return _labels[key] == value;
        }

        internal void SetLabel(PositionLabelSet labelSet)
        {
            _labels[labelSet.Key] = labelSet.Value;
        }

        public bool ContainsLabel(string key)
        {
            return _labels.ContainsKey(key);
        }

        internal void DeleteLabel(PositionLabelDeleted labelDeleted)
        {
            _labels.Remove(labelDeleted.Key);
        }

        public string GetLabelValue(string key) => _labels[key];
    }
}