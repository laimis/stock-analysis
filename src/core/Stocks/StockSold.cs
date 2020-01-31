using System;
using core.Shared;

namespace core.Stocks
{
    internal class StockSold : AggregateEvent
    {
        public StockSold(Guid id, Guid aggregateId, DateTimeOffset when, string ticker, int numberOfShares, double price)
            : base(id, aggregateId, when)
        {
            this.Ticker = ticker;
            this.NumberOfShares = numberOfShares;
            this.Price = price;
        }

        public string Ticker { get; }
        public int NumberOfShares { get; }
        public double Price { get; }
    }
}