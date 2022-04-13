using System;

namespace core.Stocks
{
    // PositionInstance models a stock position from the time the first share is opened
    // to the time when the last share is sold.
    public class PositionInstance
    {
        private DateTimeOffset? _firstOpen = null;
        public PositionInstance(string ticker)
        {
            Ticker = ticker;
        }

        public decimal NumberOfShares { get; private set; } = 0;
        public int DaysHeld => _firstOpen != null ? (int)((!IsClosed ? DateTimeOffset.UtcNow : Closed.Value).Subtract(_firstOpen.Value)).TotalDays : 0;
        public decimal Cost { get; private set; } = 0;
        public decimal Return { get; private set; } = 0;
        public decimal Percentage => Cost == 0 ? 0 : Math.Round((Return - Cost) / Cost, 4);
        public decimal Profit => Return - Cost;
        public bool IsClosed => Closed != null;
        public string Ticker { get; }
        public DateTimeOffset? Closed { get; private set; }
        public decimal MaxNumberOfShares { get; private set; }
        public decimal MaxCost { get; private set; }

        public void Buy(decimal numberOfShares, decimal price, DateTimeOffset when)
        {
            if (NumberOfShares == 0)
            {
                _firstOpen = when;
            }

            NumberOfShares += numberOfShares;
            Cost += numberOfShares * price;

            if (NumberOfShares > MaxNumberOfShares)
            {
                MaxNumberOfShares = NumberOfShares;
            }

            if (Cost > MaxCost)
            {
                MaxCost = Cost;
            }
        }

        public void Sell(decimal amount, decimal price, DateTimeOffset when)
        {
            NumberOfShares -= amount;

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