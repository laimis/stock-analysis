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
            UserId = userId;
        }

        public Guid UserId { get; }
    }

    internal class AlertCreated : AggregateEvent
    {
        public AlertCreated(Guid id, Guid aggregateId, DateTimeOffset when, string ticker, Guid userId)
            : base(id, aggregateId, when)
        {
            Ticker = ticker;
            UserId = userId;
        }

        public string Ticker { get; }
        public Guid UserId { get; }
    }

    internal class AlertPricePointAdded : AggregateEvent
    {
        public AlertPricePointAdded(Guid id, Guid aggregateId, DateTimeOffset when, decimal value)
            : base(id, aggregateId, when)
        {
            Value = value;
        }

        public decimal Value { get; }
    }

    public class AlertPricePointRemoved : AggregateEvent, INotification
    {
        public AlertPricePointRemoved(Guid id, Guid aggregateId, DateTimeOffset when, Guid pricePointId)
            : base(id, aggregateId, when)
        {
            PricePointId = pricePointId;
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
            decimal value)
            : base(id, aggregateId, when)
        {
            Description = description;
            Value = value;
        }

        public string Description { get; }
        public decimal Value { get; }
    }
}