using System;
using core.Shared;
using MediatR;

namespace core.Alerts
{
    public class AlertPricePointRemoved : AggregateEvent, INotification
    {
        public AlertPricePointRemoved(Guid id, Guid aggregateId, DateTimeOffset when, Guid pricePointId)
            : base(id, aggregateId, when)
        {
            this.PricePointId = pricePointId;
        }

        public Guid PricePointId { get; }
    }
}