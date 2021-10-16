using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks.View
{
    public class StockOwnershipView
    {
        public Guid Id { get; set; }
        public decimal AverageCost { get; set; }
        public decimal Cost { get; set; }
        public decimal Owned { get; set; }
        public string Ticker { get; set; }
        public string Category { get; set; }
        public List<Transaction> Transactions { get; set; }
    }
}