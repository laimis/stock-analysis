using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Alerts
{
    public class AlertState : IAggregateState
    {
        public Guid Id { get; private set; }
        public Ticker Ticker { get; private set; }
        public Guid UserId { get; private set; }
        public DateTimeOffset Created { get; private set; }
        public List<AlertPricePoint> PricePoints { get; private set; } = new List<AlertPricePoint>();

        public void Apply(AggregateEvent e)
        {
            ApplyInternal(e);
        }

        private void ApplyInternal(dynamic obj)
        {
            ApplyInternal(obj);
        }

        internal void ApplyInternal(AlertCleared c)
        {
        }

        internal void ApplyInternal(AlertCreated c)
        {
            Id = c.AggregateId;
            Ticker = c.Ticker;
            UserId = c.UserId;
            Created = c.When;
        }

        internal void ApplyInternal(AlertPricePointAdded a)
        {
            PricePoints.Add(new AlertPricePoint(a.Id, null, a.Value));
        }

        internal void ApplyInternal(AlertPricePointWithDescripitionAdded a)
        {
            PricePoints.Add(new AlertPricePoint(a.Id, a.Description, a.Value));
        }

        internal void ApplyInternal(AlertPricePointRemoved a)
        {
            var pp = PricePoints.Single(p => p.Id == a.PricePointId);

            PricePoints.Remove(pp);
        }
    }
}