using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Stocks
{
    public class OwnedStock : Aggregate
    {
        public OwnedStockState State { get; } = new OwnedStockState();
        public override IAggregateState AggregateState => State;
        
        public OwnedStock(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public OwnedStock(Ticker ticker, Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing user id");
            }

            Apply(new TickerObtained(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, ticker, userId));
        }

        public void Purchase(int numberOfShares, double price, DateTimeOffset date, string notes = null)
        {
            if (price <= 0)
            {
                throw new InvalidOperationException("Price cannot be empty or zero");
            }

            if (date == DateTime.MinValue)
            {
                throw new InvalidOperationException("Purchase date not specified");
            }

            Apply(
                new StockPurchased(
                    Guid.NewGuid(),
                    this.State.Id,
                    date,
                    this.State.UserId,
                    this.State.Ticker,
                    numberOfShares,
                    price,
                    notes
                )
            );
        }

        public void DeleteTransaction(Guid transactionId)
        {
            if (!this.State.BuyOrSell.Any(t => t.Id == transactionId))
            {
                throw new InvalidOperationException("Unable to find transcation to delete using id " + transactionId);
            }

            Apply(
                new StockTransactionDeleted(
                    Guid.NewGuid(),
                    this.State.Id,
                    transactionId,
                    DateTimeOffset.UtcNow
                    
                )
            );
        }

        public void Delete()
        {
            Apply(
                new StockDeleted(
                    Guid.NewGuid(),
                    this.State.Id,
                    DateTimeOffset.UtcNow
                )
            );
        }

        public void Sell(int numberOfShares, double price, DateTimeOffset date, string notes)
        {
            if (numberOfShares > this.State.Owned)
            {
                throw new InvalidOperationException("Number of shares owned is less than what is desired to sell");
            }

            Apply(
                new StockSold(
                    Guid.NewGuid(),
                    this.State.Id,
                    date,
                    this.State.UserId,
                    this.State.Ticker,
                    numberOfShares,
                    price,
                    notes)
            );
        }
    }
}