using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Stocks
{
    public record struct PositionEvent (string description, string type, decimal? value, DateTimeOffset when)
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
        public decimal GainPct => Math.Round(
            (AverageSaleCostPerShare - AverageBuyCostPerShare) * 100 / AverageBuyCostPerShare,
            2);
            
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
        public bool IsClosed => Closed != null;

        public int PositionId { get; }
        public string Ticker { get; }
        public DateTimeOffset? Closed { get; private set; }
        public decimal? FirstBuyCost { get; private set; }
        public decimal? FirstBuyNumberOfShares { get; private set; }
        public decimal? FirstStop { get; private set; }
        public decimal? RiskedAmount { get; private set; }
        
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

        public List<decimal> RRLevels { get; private set; } = new List<decimal>();
        public decimal? GetRRLevel(int index) => index < RRLevels.Count ? RRLevels[index] : null;

        public void Buy(decimal numberOfShares, decimal price, DateTimeOffset when, Guid transactionId, string notes = null)
        {
            if (NumberOfShares == 0)
            {
                Opened = when;
            }

            Transactions.Add(new PositionTransaction(numberOfShares, price, transactionId: transactionId, type:"buy", when));

            if (FirstBuyCost == null)
            {
                FirstBuyCost = price;
                FirstBuyNumberOfShares = numberOfShares;
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

            // if we haven't set the risked amount, when we set it at 5% from the first buy price?
            if (StopPrice == null)
            {
                SetStopPrice(FirstBuyCost.Value * 0.95m, when);
            }

            Transactions.Add(new PositionTransaction(numberOfShares, price, transactionId:transactionId, type: "sell", when));

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

        public void SetStopPrice(decimal? stopPrice, DateTimeOffset when)
        {
            if (stopPrice != null)
            {
                StopPrice = stopPrice;

                if (FirstStop == null)
                {
                    FirstStop = stopPrice;
                }

                Events.Add(new PositionEvent($"Stop price set to {stopPrice}", "stop", stopPrice, when));

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
            RRLevels.Clear();

            Events.Add(new PositionEvent("Stop price deleted", "stop", null, when));
        }

        public void SetRiskAmount(decimal riskAmount, DateTimeOffset when)
        {
            RiskedAmount = riskAmount;

            Events.Add(new PositionEvent("Set risk amount", "risk", riskAmount, when));

            // setting risk, should calculate the RR levels for selling to accomodate this risk
            if (NumberOfShares == 0)
            {
                return;
            }

            var riskPerShare = riskAmount / NumberOfShares;

            this.RRLevels.Clear();
            this.RRLevels.AddRange(
                new [] {1m,2m,3m,4m}
                .Select(x => AverageBuyCostPerShare + x * riskPerShare)
            );
        }

        public void SetPrice(decimal price)
        {
            Price = price;
            UnrealizedProfit = _slots.Select(cost => price - cost).Sum();
            UnrealizedGainPct = Math.Round(
                (price - AverageBuyCostPerShare) / AverageBuyCostPerShare * 100,
                2);
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