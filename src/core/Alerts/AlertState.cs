using System;
using core.Shared;

namespace core.Alerts
{
    public class AlertState
    {
        public Guid Id { get; private set; }
        public Ticker Ticker { get; private set; }
        public double Threshold { get; private set; }
        public Guid UserId { get; private set; }

        internal void Apply(AlertCreated c)
        {
            this.Id = c.AggregateId;
            this.Ticker = c.Ticker;
            this.Threshold = c.Threshold;
            this.UserId = c.UserId;
        }
    }
}