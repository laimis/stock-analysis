using System;

namespace core
{
    public struct Price
    {
        public decimal Amount { get; }

        public Price(decimal amount)
        {
            Amount = amount;
        }

        public bool NotFound => Amount == 0;
    }

    [Obsolete("Used to recover note")]
    public struct TickerPrice
    {
        public decimal Amount { get; }

        public TickerPrice(decimal amount)
        {
            Amount = amount;
        }

        public bool NotFound => Amount == 0;
    }
}