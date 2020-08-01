using System;
using core.Shared;
using MediatR;

namespace core.Alerts
{
    // DO NOT USE
    public class AlertCleared : AggregateEvent
    {
        public AlertCleared(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId)
            : base(id, aggregateId, when)
        {
            this.UserId = userId;
        }

        public Guid UserId { get; }
    }

    internal class AlertCreated : AggregateEvent
    {
        public AlertCreated(Guid id, Guid aggregateId, DateTimeOffset when, string ticker, Guid userId)
            : base(id, aggregateId, when)
        {
            this.Ticker = ticker;
            this.UserId = userId;
        }

        public string Ticker { get; }
        public Guid UserId { get; }
    }

    internal class AlertPricePointAdded : AggregateEvent
    {
        public AlertPricePointAdded(Guid id, Guid aggregateId, DateTimeOffset when, double value)
            : base(id, aggregateId, when)
        {
            this.Value = value;
        }

        public double Value { get; }
    }

    public class AlertPricePointRemoved : AggregateEvent, INotification
    {
        public AlertPricePointRemoved(Guid id, Guid aggregateId, DateTimeOffset when, Guid pricePointId)
            : base(id, aggregateId, when)
        {
            this.PricePointId = pricePointId;
        }

        public Guid PricePointId { get; }
    }

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