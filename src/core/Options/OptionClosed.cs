using System;
using core.Shared;

namespace core.Options
{
    public class OptionClosed : AggregateEvent
    {
        public OptionClosed(string ticker, string userId, int amount, double money, DateTimeOffset when)
             : base(ticker, userId, when.DateTime)
        {
            this.Amount = amount;
            this.Money = money;
        }

        public int Amount { get; }
        public double Money { get; }
    }
}