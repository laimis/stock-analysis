using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Stocks
{
    public struct PositionTransaction
    {
        public PositionTransaction(decimal numberOfShares, decimal price, Guid transactionId, string type, DateTimeOffset when)
        {
            NumberOfShares = numberOfShares;
            Price = price;
            TransactionId = transactionId;
            Type = type;
            When = when;
        }

        public decimal Price { get; }
        public Guid TransactionId { get; }
        public DateTimeOffset When { get; }
        public decimal NumberOfShares { get; }
        public string Type { get; set; }
    }

    // PositionInstance models a stock position from the time the first share is opened
    // to the time when the last share is sold.
    public class PositionInstance
    {
        public PositionInstance(string ticker)
        {
            Ticker = ticker;
        }

        public decimal NumberOfShares { get; private set; } = 0;
        public decimal AverageCostPerShare { get; private set; } = 0;
        public decimal AverageSaleCostPerShare { get; private set; } = 0;
        public decimal AverageBuyCostPerShare { get; private set; } = 0;
        public DateTimeOffset? Opened { get; private set; }
        public int DaysHeld => Opened != null ? (int)((!IsClosed ? DateTimeOffset.UtcNow : Closed.Value).Subtract(Opened.Value)).TotalDays : 0;
        public decimal Cost { get; private set; } = 0;
        public decimal Profit { get; private set; } = 0;
        public decimal GainPct => (AverageSaleCostPerShare - AverageBuyCostPerShare) / AverageBuyCostPerShare;
        public decimal RR => RiskedAmount switch {
            not null => Profit / RiskedAmount.Value,
            _ => 0
        };
        public decimal RRWeighted => RR * Cost;

        public decimal? Price { get; private set; }
        public decimal UnrealizedProfit { get; private set; } = 0;
        public decimal UnrealizedGainPct { get; private set; } = 0;
        public decimal UnrealizedRR { get; private set; } = 0;
        
        public bool IsClosed => Closed != null;
        public string Ticker { get; }
        public DateTimeOffset? Closed { get; private set; }
        public decimal? FirstBuyCost { get; private set; }
        public decimal? FirstBuyNumberOfShares { get; private set; }
        public decimal? RiskedAmount { get; private set; }
        
        public List<PositionTransaction> Transactions { get; private set; } = new List<PositionTransaction>();

        public List<string> Notes { get; private set; } = new List<string>();
        public decimal? StopPrice { get; private set; }
        public DateTimeOffset LastTransaction { get; private set; }
        public int DaysSinceLastTransaction => (int)(DateTimeOffset.UtcNow - LastTransaction).TotalDays;
        public string Category { get; private set; }
        private List<decimal> _slots = new List<decimal>();

        public void SetCategory(string category) => Category = category;

        // RRLevels is a concept that I am thinking about right now (09/2022). It's taking
        // the risked amount and creating internvals to the upside by a risked amount per share,
        // based on the initial purchase price
        public List<decimal> RRLevels
        {
            get
            {
                if (NumberOfShares == 0)
                {
                    return new List<decimal>();
                }

                var riskedAmount = RiskedAmount switch {
                    not null => RiskedAmount.Value,
                    _ => FirstBuyCost.Value * 0.05m * FirstBuyNumberOfShares.Value
                };

                var riskLeft = riskedAmount - Profit;
                var riskPerShare = riskLeft / NumberOfShares;

                return new [] {1m,2m,3m,4m}
                    .Select(x => riskPerShare * x + AverageCostPerShare)
                    .ToList();
            }
        }

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
            var tx = Transactions.FirstOrDefault(t => t.TransactionId == transactionId);
            if (tx.TransactionId == null)
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
                SetStopPrice(FirstBuyCost.Value * 0.95m);
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

        public void SetStopPrice(decimal? stopPrice)
        {
            if (stopPrice != null)
            {
                StopPrice = stopPrice;

                if (RiskedAmount == null)
                {
                    RiskedAmount = (AverageCostPerShare - stopPrice.Value) * NumberOfShares;
                }
            }
        }

        public void SetRiskAmount(decimal riskAmount)
        {
            RiskedAmount = riskAmount;
        }

        internal void SetPrice(decimal price)
        {
            Price = price;
            UnrealizedProfit = _slots.Select(cost => price - cost).Sum();
            UnrealizedGainPct = (price - AverageBuyCostPerShare) / AverageBuyCostPerShare;
            UnrealizedRR = RiskedAmount switch {
                not null => UnrealizedProfit / RiskedAmount.Value,
                _ => 0
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