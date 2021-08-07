using System;

namespace core.Stocks
{
    // PositionInstance models a stock position from the time the first share is opened
    // to the time when the last share is sold.
    public class PositionInstance
    {
        private DateTimeOffset? _firstOpen = null;
        private int _amount = 0;

        public PositionInstance(string ticker)
        {
            Ticker = ticker;
        }

        public int DaysHeld => _firstOpen != null ? (int)((!IsClosed ? DateTimeOffset.UtcNow : Closed.Value).Subtract(_firstOpen.Value)).TotalDays : 0;
        public double Cost { get; private set; } = 0;
        public double Return { get; private set; } = 0;
        public double Percentage => Cost == 0 ? 0 : Math.Round((Return - Cost) / Cost, 4);
        public double Profit => Return - Cost;
        public bool IsClosed => Closed != null;
        public string Ticker { get; }
        public DateTimeOffset? Closed { get; private set; }

        public void Buy(int amount, double price, DateTimeOffset when)
        {
            if (_amount == 0)
            {
                _firstOpen = when;
            }

            _amount += amount;

            Cost += amount * price;
        }

        public void Sell(int amount, double price, DateTimeOffset when)
        {
            _amount -= amount;

            if (_amount < 0)
            {
                throw new InvalidOperationException("Transaction would make amount owned invalid");
            }

            if (_amount == 0)
            {
                Closed = when;
            }

            Return += amount * price;
        }
    }
}