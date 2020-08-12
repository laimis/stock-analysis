using System;
using System.Collections.Generic;
using System.Linq;
using core.Portfolio.Output;

namespace core.Portfolio
{
    public class ReviewList
    {
        public ReviewList(
            DateTimeOffset start,
            DateTimeOffset end,
            List<ReviewEntryGroup> entries,
            TransactionList stocks,
            TransactionList options)
        {
            this.Start = start;
            this.End = end;
            this.Entries = entries.OrderBy(e => e.Ticker);
            this.ShortEarnings = entries.Where(g => g.EarningsDaysLeft >= 0 && g.EarningsDaysLeft <= 7).OrderBy(e => e.EarningsDate);
            this.LongEarnings = entries.Where(g => g.EarningsDaysLeft > 7 && g.EarningsDaysLeft <= 35).OrderBy(e => e.EarningsDate);
            this.Stocks = stocks;
            this.Options = options;
        }
        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
        public IEnumerable<ReviewEntryGroup> Entries { get; }
        public IEnumerable<ReviewEntryGroup> ShortEarnings { get; }
        public IEnumerable<ReviewEntryGroup> LongEarnings { get; }
        public TransactionList Stocks { get; }
        public TransactionList Options { get; }
    }
}