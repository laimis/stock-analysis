using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Stocks
{
    public struct PositionTransaction
    {
        public PositionTransaction(decimal quantity, decimal price, DateTimeOffset when)
        {
            Quantity = quantity;
            Price = price;
            When = when;
        }

        public decimal Price { get; }
        public DateTimeOffset When { get; }
        public decimal Quantity { get; }
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
        public DateTimeOffset? Opened { get; private set; }
        public int DaysHeld => Opened != null ? (int)((!IsClosed ? DateTimeOffset.UtcNow : Closed.Value).Subtract(Opened.Value)).TotalDays : 0;
        public decimal Cost { get; private set; } = 0;
        public decimal Return { get; private set; } = 0;
        public decimal ReturnPct => Cost == 0 ? 0 : Math.Round((Return - Cost) / Cost, 4);
        public decimal Profit => Return - Cost;
        public bool IsClosed => Closed != null;
        public string Ticker { get; }
        public DateTimeOffset? Closed { get; private set; }
        public decimal MaxNumberOfShares { get; private set; }
        public int NumberOfBuys { get; private set; }
        public int NumberOfSells { get; private set; }
        public decimal? FirstBuyCost { get; private set; }
        public List<PositionTransaction> Buys { get; private set; } = new List<PositionTransaction>();
        public List<PositionTransaction> Sells { get; private set; } = new List<PositionTransaction>();
        public List<string> Notes { get; private set; } = new List<string>();
        public decimal? StopPrice { get; private set; }
        public decimal AverageCost => BuysForAverageCostCalculations.Aggregate(0m, (a, b) => a +  b.Item1 * b.Item2) / 
            BuysForAverageCostCalculations.Sum(b => b.Item1);
        private List<(decimal, decimal)> BuysForAverageCostCalculations { get; set; } = new List<(decimal, decimal)>();

        internal void ApplyPrice(decimal currentPrice)
        {
            Price = currentPrice;
            UnrealizedGain = (Price - AverageCost) * NumberOfShares;
            UnrealizedGainPct = (Price - AverageCost) / AverageCost;
        }

        public decimal PotentialLoss => NumberOfShares * AverageCost * RiskedPct;

        public decimal RiskedPct => StopPrice switch {
            not null => (AverageCost - StopPrice.Value) / AverageCost,
            null => 0.05m
        };

        public decimal RiskedAmount => Cost * RiskedPct;

        public decimal RR => Profit / RiskedAmount;
        public decimal RRWeighted => RR * Profit;
        public decimal UnrealizedRR => UnrealizedGain / RiskedAmount;
        public decimal Price { get; private set; }
        public decimal UnrealizedGain { get; private set; }
        public decimal UnrealizedGainPct { get; private set; }

        public void Buy(decimal numberOfShares, decimal price, DateTimeOffset when, string notes = null)
        {
            if (NumberOfShares == 0)
            {
                Opened = when;
            }

            BuysForAverageCostCalculations.Add((numberOfShares, price));
            NumberOfShares += numberOfShares;
            Cost += numberOfShares * price;
            NumberOfBuys++;
            
            Buys.Add(new PositionTransaction(numberOfShares, price, when));

            if (NumberOfShares > MaxNumberOfShares)
            {
                MaxNumberOfShares = NumberOfShares;
            }

            if (FirstBuyCost == null)
            {
                FirstBuyCost = price;
            }

            if (notes != null)
            {
                Notes.Add(notes);
            }
        }

        public void Sell(decimal amount, decimal price, DateTimeOffset when, string notes = null)
        {
            NumberOfShares -= amount;
            NumberOfSells++;
            Return += amount * price;
            Sells.Add(new PositionTransaction(amount, price, when));

            if (NumberOfShares < 0)
            {
                throw new InvalidOperationException("Transaction would make amount owned invalid");
            }

            if (NumberOfShares == 0)
            {
                Closed = when;
            }

            if (notes != null)
            {
                Notes.Add(notes);
            }
        }

        public void SetStopPrice(decimal? stopPrice)
        {
            if (stopPrice != null)
            {
                StopPrice = stopPrice;
            }
        }
    }
}