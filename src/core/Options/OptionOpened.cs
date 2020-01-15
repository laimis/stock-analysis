using System;
using core.Shared;

namespace core.Portfolio
{
    public class OptionOpened : AggregateEvent
    {
        // TODO: remove .DateTime
        public OptionOpened(string ticker, string userId, int amount, double premium, DateTimeOffset filled)
             : base(ticker, userId, filled.DateTime)
        {
            this.Amount = amount;
            this.Premium = premium;
            this.Filled = filled;
        }

        public int Amount { get; }
        public double Premium { get; }
        public DateTimeOffset Filled { get; }
    }
}