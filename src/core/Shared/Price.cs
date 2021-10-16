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

        public static implicit operator Price(TickerPrice t) => new Price(t.Amount);
        public static implicit operator TickerPrice(Price t) => new TickerPrice(t.Amount);
    }
}