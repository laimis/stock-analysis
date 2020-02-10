using System;
using core.Shared;
using MediatR;

namespace core.Stocks
{
    public class StockPurchased : AggregateEvent, INotification
    {
        public StockPurchased(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string ticker,
            int numberOfShares,
            double price,
            string notes)
            : base(id, aggregateId, when)
        {
            this.UserId = userId;
            this.Ticker = ticker;
            this.NumberOfShares = numberOfShares;
            this.Price = price;
            this.Notes = notes;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public int NumberOfShares { get; }
        public double Price { get; }
        public string Notes { get; }
    }
}