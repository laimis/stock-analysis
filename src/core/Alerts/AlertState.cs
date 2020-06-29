using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Alerts
{
    public class AlertState
    {
        public Guid Id { get; private set; }
        public Ticker Ticker { get; private set; }
        public Guid UserId { get; private set; }
        public List<AlertPricePoint> PricePoints { get; private set; } = new List<AlertPricePoint>();

        internal void Apply(AlertCreated c)
        {
            this.Id = c.AggregateId;
            this.Ticker = c.Ticker;
            this.UserId = c.UserId;
        }

        internal void Apply(AlertPricePointAdded a)
        {
            this.PricePoints.Add(new AlertPricePoint(a.Id, a.Value));
        }
    }
}