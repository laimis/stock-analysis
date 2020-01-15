using System;
using core.Shared;

namespace core.Portfolio
{
    internal class StockPurchased : AggregateEvent
    {
        public StockPurchased(string ticker, string userId, int amount, double price, DateTime when)
            : base(ticker, userId, when)
        {
            this.Amount = amount;
            this.Price = price;
        }

        public int Amount { get; }
        public double Price { get; }
    }
}