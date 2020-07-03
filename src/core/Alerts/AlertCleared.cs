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
}