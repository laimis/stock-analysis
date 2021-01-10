using System.Collections.Generic;
using core.Shared;

namespace core.Portfolio.Output
{
    public class ReviewTicker
    {
        public ReviewTicker(string ticker, IEnumerable<Transaction> transactions)
        {
            Ticker = ticker;
            Transactions = transactions;
        }

        public string Ticker { get; }
        public IEnumerable<Transaction> Transactions { get; }
    }
}