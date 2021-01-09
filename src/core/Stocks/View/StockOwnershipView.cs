using System;
using core.Portfolio.Output;

namespace core.Stocks.View
{
    internal class StockOwnershipView
    {
        public Guid Id { get; set; }
        public double AverageCost { get; set; }
        public double Cost { get; set; }
        public int Owned { get; set; }
        public string Ticker { get; set; }
        public string Category { get; set; }
        public TransactionList Transactions { get; set; }
    }
}