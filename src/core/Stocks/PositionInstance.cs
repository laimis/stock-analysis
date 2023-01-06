using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Stocks
{
    public enum PositionEventType { buy, stop, sell, risk }
    public record struct PositionEvent (string description, PositionEventType type, decimal? value, DateTimeOffset when)
    {
        public string Date => when.ToString("yyyy-MM-dd");
    }
    public record struct PositionTransaction(decimal numberOfShares, decimal price, Guid transactionId, string type, DateTimeOffset when)
    {
        public string Date => when.ToString("yyyy-MM-dd");
        public int AgeInDays => (int)(DateTimeOffset.Now - when).TotalDays;
    }

    // PositionInstance models a stock position from the time the first share is opened
    // to the time when the last share is sold.
    public class PositionInstance
    {
        public PositionInstance(int positionId, string ticker)
        {
            PositionId = positionId;
            Ticker = ticker;
            Category = StockCategory.ShortTerm;
        }

        public decimal NumberOfShares { get; private set; } = 0;
        public decimal AverageCostPerShare { get; private set; } = 0;
        public decimal AverageSaleCostPerShare { get; private set; } = 0;
        public decimal AverageBuyCostPerShare { get; private set; } = 0;
        public DateTimeOffset? Opened { get; private set; }
        public int DaysHeld => Opened != null ? (int)((!IsClosed ? DateTimeOffset.UtcNow : Closed.Value).Subtract(Opened.Value)).TotalDays : 0;
        public decimal Cost { get; private set; } = 0;
        public decimal Profit { get; private set; } = 0;
        public decimal GainPct => IsClosed switch {
            true => (AverageSaleCostPerShare - AverageBuyCostPerShare) / AverageBuyCostPerShare,
            false => UnrealizedGainPct.HasValue ? UnrealizedGainPct.Value : 0
        };
            
        public decimal RR => RiskedAmount switch {
            not null => Profit / RiskedAmount.Value + (UnrealizedRR.HasValue ? UnrealizedRR.Value : 0),
            _ => 0
        };
        public decimal RRWeighted => RR * Cost;

        public decimal? Price { get; private set; }
        public decimal? UnrealizedProfit { get; private set; } = null;
        public decimal? UnrealizedGainPct { get; private set; } = null;
        public decimal? UnrealizedRR { get; private set; } = null;
        public decimal? PercentToStop { get; private set; } = null;
        public decimal CombinedProfit => Profit + (UnrealizedProfit ?? 0);
        public bool IsClosed => Closed != null;

        public int PositionId { get; }
        public string Ticker { get; }
        public DateTimeOffset? Closed { get; private set; }
        public decimal? FirstStop { get; private set; }
        public decimal? RiskedAmount { get; private set; }
        public decimal? CostAtRiskedBasedOnStopPrice => StopPrice switch {
            not null => StopPrice > AverageCostPerShare ? 0 : (AverageCostPerShare - StopPrice) * NumberOfShares,
            _ => null
        };
        
        public List<PositionTransaction> Transactions { get; private set; } = new List<PositionTransaction>();
        public List<PositionEvent> Events { get; private set; } = new List<PositionEvent>();

        public List<string> Notes { get; private set; } = new List<string>();
        public decimal? StopPrice { get; private set; }
        public DateTimeOffset LastTransaction { get; private set; }
        public int DaysSinceLastTransaction => (int)(DateTimeOffset.UtcNow - LastTransaction).TotalDays;
        public string Category { get; private set; }
        public void SetCategory(string category) => Category = category;
        public bool IsShortTerm => Category == null ||  Category == StockCategory.ShortTerm;

        private List<decimal> _slots = new List<decimal>();

        private bool PositionCompleted = false;
        private decimal CompletedPositionCost = 0;
        public decimal CompletedPositionShares = 0;
        public decimal CompletedPositionCostPerShare => CompletedPositionCost / CompletedPositionShares;

        public void Buy(decimal numberOfShares, decimal price, DateTimeOffset when, Guid transactionId, string notes = null)
        {
            if (NumberOfShares == 0)
            {
                Opened = when;
            }

            Transactions.Add(new PositionTransaction(numberOfShares, price, transactionId: transactionId, type:"buy", when));
            Events.Add(new PositionEvent($"buy {numberOfShares} @ ${price}", PositionEventType.buy, price, when));

            if (PositionCompleted == false)
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
            var tx = Transactions.FirstOrDefault(t => t.transactionId == transactionId);
            if (tx.transactionId == null)
            {
                throw new InvalidOperationException($"Transaction {transactionId} not found");
            }

            Transactions.Remove(tx);

            RunCalculations();
        }

        public void Sell(decimal numberOfShares, decimal price, Guid transactionId, DateTimeOffset when, string notes = null)
        {
            if (NumberOfShares <= 0)
            {
                throw new InvalidOperationException("Transaction would make amount owned invalid");
            }

            PositionCompleted = true;

            Transactions.Add(new PositionTransaction(numberOfShares, price, transactionId:transactionId, type: "sell", when));
            Events.Add(new PositionEvent($"sell {numberOfShares} @ ${price}", PositionEventType.sell, price, when));

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

            RunCalculations();

            if (NumberOfShares == 0)
            {
                Closed = when;
            }
        }

        private bool HasEventWithDescription(string testDescription) => Events.Any(e => e.description == testDescription);

        public void SetStopPrice(decimal? stopPrice, DateTimeOffset when)
        {
            if (stopPrice != null && stopPrice != this.StopPrice)
            {
                StopPrice = stopPrice;

                if (FirstStop == null)
                {
                    FirstStop = stopPrice;
                }

                PositionCompleted = true;

                var stopPercentage = Math.Round((AverageCostPerShare - stopPrice.Value) / AverageCostPerShare * 100, 2);

                Events.Add(new PositionEvent($"Stop price set to {stopPrice} ({stopPercentage}%)", PositionEventType.stop, stopPrice, when));

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
            
            Events.Add(new PositionEvent("Stop price deleted", PositionEventType.stop, null, when));
        }

        public void SetRiskAmount(decimal riskAmount, DateTimeOffset when)
        {
            RiskedAmount = riskAmount;

            Events.Add(new PositionEvent("Set risk amount", PositionEventType.risk, riskAmount, when));
        }

        public void SetPrice(decimal price)
        {
            Price = price;
            UnrealizedProfit = _slots.Select(cost => price - cost).Sum();
            UnrealizedGainPct = (price - AverageCostPerShare) / AverageCostPerShare;
            UnrealizedRR = RiskedAmount switch {
                not null => UnrealizedProfit / RiskedAmount.Value,
                _ => 0
            };
            PercentToStop = StopPrice switch {
                not null => (StopPrice.Value - price) / StopPrice.Value,
                _ => -1
            };
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
            if (transaction.type == "buy")
            {
                for (var i = 0; i < transaction.numberOfShares; i++)
                {
                    _slots.Add(transaction.price);
                    cost += transaction.price;
                    numberOfShares++;
                }

                totalBuy += transaction.price * transaction.numberOfShares;
                totalNumberOfSharesBought += transaction.numberOfShares;
            }
            else
            {
                // remove quantity number of slots from the beginning of an array
                var removed = _slots.Take((int)transaction.numberOfShares).ToList();
                _slots.RemoveRange(0, (int)transaction.numberOfShares);
                removed.ForEach(
                    removedElement =>
                    {
                        profit += transaction.price - removedElement;
                        cost -= removedElement;
                        numberOfShares--;
                    }
                );
                
                totalSale += transaction.price * transaction.numberOfShares;
                totalNumberOfSharesSold += transaction.numberOfShares;
            }
            });

            // calculate average cost per share using slots
            this.AverageCostPerShare = _slots.Count switch {
                0 => 0,
                _ => _slots.Sum() / _slots.Count
            };
            this.Cost = cost;
            this.Profit = profit;
            this.NumberOfShares = numberOfShares;

            this.AverageSaleCostPerShare = totalNumberOfSharesSold switch {
                0 => 0,
                _ => totalSale / totalNumberOfSharesSold
            };

            this.AverageBuyCostPerShare = totalNumberOfSharesBought switch {
                0 => 0,
                _ => totalBuy / totalNumberOfSharesBought
            };
        }
    }
}