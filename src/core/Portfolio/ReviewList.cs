using System;
using System.Collections.Generic;
using core.Portfolio.Output;

namespace core.Portfolio
{
    public class ReviewList
    {
        public ReviewList(
            DateTimeOffset start,
            DateTimeOffset end,
            List<ReviewEntryGroup> tickers,
            TransactionList transactions)
        {
            this.Start = start;
            this.End = end;
            this.Entries = tickers;
            this.Transactions = transactions;
        }

        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
        public List<ReviewEntryGroup> Entries { get; }
        public TransactionList Transactions { get; }
    }
}