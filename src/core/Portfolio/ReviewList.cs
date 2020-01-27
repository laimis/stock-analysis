using System.Collections.Generic;
using core.Portfolio.Output;

namespace core.Portfolio
{
    public struct ReviewList
    {
        public List<ReviewEntryGroup> Tickers { get; internal set; }
        public TransactionList TransactionList { get; internal set; }
    }
}