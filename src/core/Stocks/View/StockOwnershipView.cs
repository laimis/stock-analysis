using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks.View
{
    internal class StockOwnershipView
    {
        public Guid Id { get; set; }
        public decimal AverageCost { get; set; }
        public decimal Cost { get; set; }
        public int Owned { get; set; }
        public string Ticker { get; set; }
        public string Category { get; set; }
        public List<Transaction> Transactions { get; set; }
    }
}