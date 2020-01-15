using System;
using core.Shared;

namespace core.Options
{
    public class OptionClosed : AggregateEvent
    {
        public OptionClosed(string ticker, string userId, int amount, double money, DateTimeOffset closed)
             : base(ticker, userId, closed.DateTime)
        {
            this.Amount = amount;
            this.Money = money;
        }

        public int Amount { get; }
        public double Money { get; }
    }
}