using System;

namespace core.Stocks
{
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
        public decimal Percentage => Cost == 0 ? 0 : Math.Round((Return - Cost) / Cost, 4);
        public decimal Profit => Return - Cost;
        public bool IsClosed => Closed != null;
        public string Ticker { get; }
        public DateTimeOffset? Closed { get; private set; }
        public decimal MaxNumberOfShares { get; private set; }
        public decimal MaxCost { get; private set; }
        public int NumberOfBuys { get; private set; }
        public int NumberOfSells { get; private set; }
        public decimal? FirstBuyCost { get; private set; }

        public void Buy(decimal numberOfShares, decimal price, DateTimeOffset when)
        {
            if (NumberOfShares == 0)
            {
                Opened = when;
            }

            NumberOfShares += numberOfShares;
            Cost += numberOfShares * price;
            NumberOfBuys++;

            if (NumberOfShares > MaxNumberOfShares)
            {
                MaxNumberOfShares = NumberOfShares;
            }

            if (Cost > MaxCost)
            {
                MaxCost = Cost;
            }

            if (FirstBuyCost == null)
            {
                FirstBuyCost = price;
            }
        }

        public void Sell(decimal amount, decimal price, DateTimeOffset when)
        {
            NumberOfShares -= amount;
            NumberOfSells++;

            if (NumberOfShares < 0)
            {
                throw new InvalidOperationException("Transaction would make amount owned invalid");
            }

            if (NumberOfShares == 0)
            {
                Closed = when;
            }

            Return += amount * price;
        }
    }
}