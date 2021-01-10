using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Admin
{
    public class EmailReviewList
    {
        public EmailReviewList(
            DateTimeOffset start,
            DateTimeOffset end,
            List<EmailReviewEntryGroup> entries,
            EmailTransactionList stocks,
            EmailTransactionList options)
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
        public IEnumerable<EmailReviewEntryGroup> Entries { get; }
        public IEnumerable<EmailReviewEntryGroup> ShortEarnings { get; }
        public IEnumerable<EmailReviewEntryGroup> LongEarnings { get; }
        public EmailTransactionList Stocks { get; }
        public EmailTransactionList Options { get; }
    }
}