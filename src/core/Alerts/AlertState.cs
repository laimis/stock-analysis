using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Alerts
{
    public class AlertState
    {
        public Guid Id { get; private set; }
        public Ticker Ticker { get; private set; }
        public Guid UserId { get; private set; }
        public DateTimeOffset Created { get; private set; }
        public List<AlertPricePoint> PricePoints { get; private set; } = new List<AlertPricePoint>();

        internal void Apply(AlertCreated c)
        {
            this.Id = c.AggregateId;
            this.Ticker = c.Ticker;
            this.UserId = c.UserId;
            this.Created = c.When;
        }

        internal void Apply(AlertPricePointAdded a)
        {
            this.PricePoints.Add(new AlertPricePoint(a.Id, null, a.Value));
        }

        internal void Apply(AlertPricePointWithDescripitionAdded a)
        {
            this.PricePoints.Add(new AlertPricePoint(a.Id, a.Description, a.Value));
        }

        internal void Apply(AlertPricePointRemoved a)
        {
            var pp = this.PricePoints.Single(p => p.Id == a.PricePointId);

            this.PricePoints.Remove(pp);
        }
    }
}