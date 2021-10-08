using System;
using System.Collections.Generic;
using System.Linq;
using core.Adapters.Stocks;

namespace core.Portfolio
{
    public struct GridEntry
    {
        public GridEntry(string ticker, Price price, StockAdvancedStats stats)
        {
            this.Price = price.Amount;
            this.Ticker = ticker;
            this.Stats = stats;
        }

        public string Ticker { get; }
        public StockAdvancedStats Stats { get; }
        public decimal Price { get; }
    }
}