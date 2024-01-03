using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks
{
    public class PendingStockPosition : Aggregate<PendingStockPositionState>
    {
        public PendingStockPosition(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public PendingStockPosition(
            string notes,
            decimal numberOfShares,
            decimal price,
            decimal? stopPrice,
            string strategy,
            Ticker ticker,
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing user id");
            }

            if (price <= 0)
            {
                throw new InvalidOperationException("Price cannot be negative or zero");
            }

            if (stopPrice.HasValue && stopPrice.Value < 0)
            {
                throw new InvalidOperationException("Stop price cannot be negative or zero");
            }

            if (string.IsNullOrWhiteSpace(notes))
            {
                throw new InvalidOperationException("Notes cannot be blank");
            }

            if (string.IsNullOrWhiteSpace(strategy))
            {
                throw new InvalidOperationException("Strategy cannot be blank");
            }

            Apply(new PendingStockPositionCreatedWithStrategy(
                Guid.NewGuid(),
                Guid.NewGuid(),
                when: DateTimeOffset.UtcNow,
                userId: userId,
                ticker: ticker.Value,
                price: price,
                numberOfShares: numberOfShares,
                stopPrice: stopPrice,
                notes: notes,
                strategy: strategy)
            );
        }
        
        public void Purchase(decimal price)
        {
            if (price <= 0)
            {
                throw new InvalidOperationException("Price cannot be negative or zero");
            }

            Apply(
                new PendingStockPositionClosed(
                    Guid.NewGuid(),
                    State.Id,
                    when : DateTimeOffset.UtcNow,
                    purchased: true,
                    price: price
                )
            );
        }

        public void Close()
        {
            if (State.IsClosed)
            {
                return;
            }
            
            Apply(
                new PendingStockPositionClosed(
                    Guid.NewGuid(),
                    State.Id,
                    when : DateTimeOffset.UtcNow,
                    purchased: false,
                    price: null
                )
            );
        }
    }
}