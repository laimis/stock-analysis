using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Alerts
{
    public class Alert : Aggregate
    {
        public AlertState State { get; } = new AlertState();
        public override IAggregateState AggregateState => State;

        public string Ticker => State.Ticker;
        public Guid UserId => State.UserId;
        
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

        public bool AddPricePoint(string description, double value)
        {
            var exists = this.PricePoints.Any(p => p.Value == value);
            if (exists)
            {
                return false;
            }

            Apply(
                new AlertPricePointWithDescripitionAdded(
                    Guid.NewGuid(),
                    this.Id,
                    DateTimeOffset.UtcNow,
                    description,
                    value
                )
            );

            return true;
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
    }
}