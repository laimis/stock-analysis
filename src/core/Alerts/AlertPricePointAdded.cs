using System;
using core.Shared;

namespace core.Alerts
{
    internal class AlertPricePointAdded : AggregateEvent
    {
        public AlertPricePointAdded(Guid id, Guid aggregateId, DateTimeOffset when, double value)
            : base(id, aggregateId, when)
        {
            this.Value = value;
        }

        public double Value { get; }
    }
}