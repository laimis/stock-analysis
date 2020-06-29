using System;
using core.Shared;

namespace core.Alerts
{
    internal class AlertPricePointRemoved : AggregateEvent
    {
        public AlertPricePointRemoved(Guid id, Guid aggregateId, DateTimeOffset when, Guid pricePointId)
            : base(id, aggregateId, when)
        {
            this.PricePointId = pricePointId;
        }

        public Guid PricePointId { get; }
    }
}