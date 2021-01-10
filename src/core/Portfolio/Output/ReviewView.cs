using System;
using System.Collections.Generic;

namespace core.Portfolio.Output
{
    public class ReviewView
    {
        public ReviewView(
            DateTimeOffset start,
            DateTimeOffset end,
            List<ReviewTicker> stocks,
            List<ReviewTicker> options)
        {
            Start = start;
            End = end;
            Stocks = stocks;
            Options = options;
        }
        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
        public List<ReviewTicker> Stocks { get; }
        public List<ReviewTicker> Options { get; }
    }
}