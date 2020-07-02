using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Alerts
{
    public class Alert : Aggregate
    {
        public AlertState State => _state;
        private AlertState _state = new AlertState();

        public override Guid Id => State.Id;
        public string Ticker => State.Ticker;
        private Guid UserId => State.UserId;
        
        public List<AlertPricePoint> PricePoints => State.PricePoints;
        
        public Alert(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public Alert(Ticker ticker, Guid userId)
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
                    userId)
            );
        }

        public void AddPricePoint(double value)
        {
            var exists = this.PricePoints.Any(p => p.Value == value);
            if (exists)
            {
                return;
            }

            Apply(
                new AlertPricePointAdded(
                    Guid.NewGuid(),
                    this.Id,
                    DateTimeOffset.UtcNow,
                    value
                )
            );
        }

        public void RemovePricePoint(Guid pricePointId)
        {
            var exists = this.PricePoints.Any(p => p.Id == pricePointId);
            if (!exists)
            {
                return;
            }

            Apply(
                new AlertPricePointRemoved(
                    Guid.NewGuid(),
                    this.Id,
                    DateTimeOffset.UtcNow,
                    pricePointId
                )
            );

            if (this.PricePoints.Count == 0)
            {
                Apply(
                    new AlertCleared(
                        Guid.NewGuid(),
                        this.Id,
                        DateTimeOffset.UtcNow,
                        this.UserId
                    )
                );
            }
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

        private void ApplyInternal(AlertPricePointAdded a)
        {
            this.State.Apply(a);
        }

        private void ApplyInternal(AlertPricePointRemoved a)
        {
            this.State.Apply(a);
        }

        private void ApplyInternal(AlertCleared c)
        {
        }
    }
}