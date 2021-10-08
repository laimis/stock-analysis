using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Portfolio.Output
{
    public class ReviewView
    {
        public ReviewView(
            DateTimeOffset start,
            DateTimeOffset end,
            List<ReviewTicker> stocks,
            List<ReviewTicker> options,
            List<ReviewTicker> plStocks,
            List<ReviewTicker> plOptions)
        {
            Start = start;
            End = end;
            Stocks = stocks;
            Options = options;
            PLStocks = plStocks;
            PLOptions = plOptions;

            StockProfit = plStocks.SelectMany(t => t.Transactions).Select(t => t.Profit)
                .Aggregate(0m, (sum, next) => sum + next, sum => sum);

            OptionProfit = plOptions.SelectMany(t => t.Transactions).Select(t => t.Profit)
                .Aggregate(0m, (sum, next) => sum + next, sum => sum);
        }
        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
        public List<ReviewTicker> Stocks { get; }
        public List<ReviewTicker> Options { get; }
        public List<ReviewTicker> PLStocks { get; }
        public List<ReviewTicker> PLOptions { get; }
        public decimal StockProfit { get; }
        public decimal OptionProfit { get; }
    }
}