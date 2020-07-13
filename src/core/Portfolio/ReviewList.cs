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
            TransactionList transactions)
        {
            this.Start = start;
            this.End = end;
            this.Entries = entries.OrderBy(e => e.Ticker);
            this.Transactions = transactions;
        }

        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
        public IEnumerable<ReviewEntryGroup> Entries { get; }
        public TransactionList Transactions { get; }
    }
}