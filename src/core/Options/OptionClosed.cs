using System;
using core.Shared;

namespace core.Options
{
    public class OptionClosed : AggregateEvent
    {
        public OptionClosed(Guid guid, Guid aggregateId, DateTimeOffset when, int amount, double money)
             : base(guid, aggregateId, when)
        {
            this.Amount = amount;
            this.Money = money;
        }

        public int Amount { get; }
        public double Money { get; set; }
    }
}