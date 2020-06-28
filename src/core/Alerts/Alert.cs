using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Alerts
{
    public class Alert : Aggregate
    {
        public AlertState State => _state;
        private AlertState _state = new AlertState();

        public override Guid Id => State.Id;
        
        public Alert(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public Alert(Ticker ticker, Guid userId, double threshold, bool daily)
        {
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing user id");
            }

            Apply(
                new AlertCreated(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    ticker,
                    userId,
                    threshold,
                    daily)
            );
        }

        protected override void Apply(AggregateEvent e)
        {
            this._events.Add(e);

            ApplyInternal(e);
        }

        private void ApplyInternal(dynamic obj)
        {
            this.ApplyInternal(obj);
        }

        private void ApplyInternal(AlertCreated c)
        {
            this.State.Apply(c);
        }
    }
}