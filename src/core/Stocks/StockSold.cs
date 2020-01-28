using System;
using core.Shared;

namespace core.Stocks
{
    internal class StockSold : AggregateEvent
    {
        public StockSold(Guid id, Guid aggregateId, DateTimeOffset when, string ticker, int amount, double price)
            : base(id, aggregateId, when)
        {
            this.Ticker = ticker;
            this.Amount = amount;
            this.Price = price;
        }

        public string Ticker { get; }
        public int Amount { get; }
        public double Price { get; }
    }
}