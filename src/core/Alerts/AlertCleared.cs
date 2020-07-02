using System;
using core.Shared;
using MediatR;

namespace core.Alerts
{
    public class AlertCleared : AggregateEvent, INotification
    {
        public AlertCleared(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId)
            : base(id, aggregateId, when)
        {
            this.UserId = userId;
        }

        public Guid UserId { get; }
    }
}