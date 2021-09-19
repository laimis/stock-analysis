using System;

namespace core.Cryptos
{
    // PositionInstance models a stock position from the time the first share is opened
    // to the time when the last share is sold.
    public class PositionInstance
    {
        private DateTimeOffset? _firstOpen = null;
        private double _quantity = 0;

        public PositionInstance(string token)
        {
            Token = token;
        }

        public int DaysHeld => _firstOpen != null ? (int)((!IsClosed ? DateTimeOffset.UtcNow : Closed.Value).Subtract(_firstOpen.Value)).TotalDays : 0;
        public double Cost { get; private set; } = 0;
        public double Return { get; private set; } = 0;
        public double Percentage => Cost == 0 ? 0 : Math.Round((Return - Cost) / Cost, 4);
        public double Profit => Return - Cost;
        public bool IsClosed => Closed != null;
        public string Token { get; }
        public DateTimeOffset? Closed { get; private set; }

        public void Buy(double quantity, double dollarAmountSpent, DateTimeOffset when)
        {
            if (_quantity == 0)
            {
                _firstOpen = when;
            }

            _quantity += quantity;

            Cost += dollarAmountSpent;
        }

        public void Sell(double quantity, double dollarAmountReceived, DateTimeOffset when)
        {
            _quantity -= quantity;

            if (_quantity < 0)
            {
                throw new InvalidOperationException("Transaction would make amount owned invalid");
            }

            if (_quantity == 0)
            {
                Closed = when;
            }

            Return += dollarAmountReceived;
        }
    }
}