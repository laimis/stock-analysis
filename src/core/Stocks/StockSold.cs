using System;
using core.Shared;

namespace core.Stocks
{
    internal class StockSold : AggregateEvent
    {
        public StockSold(string ticker, string userId, int amount, double price, DateTime when)
            : base(ticker, userId, when)
        {
            this.Amount = amount;
            this.Price = price;
        }

        public int Amount { get; }
        public double Price { get; }
    }
}