using System;
using core.Shared;

namespace core.Alerts
{
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
}