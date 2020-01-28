using System;
using core.Shared;

namespace core.Options
{
    public class OptionClosed : AggregateEvent
    {
        public OptionClosed(Guid id, Guid aggregateId, DateTimeOffset when, int amount, double money)
             : base(id, aggregateId, when)
        {
            this.Amount = amount;
            this.Money = money;
        }

        public int Amount { get; }
        public double Money { get; set; }
    }
}