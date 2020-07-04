using System;
using core.Shared;

namespace core.Alerts
{
    internal class AlertPricePointWithDescripitionAdded : AggregateEvent
    {
        public AlertPricePointWithDescripitionAdded(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string description,
            double value)
            : base(id, aggregateId, when)
        {
            this.Description = description;
            this.Value = value;
        }

        public string Description { get; }
        public double Value { get; }
    }
}