using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Portfolio.Output
{
    public class TransactionList
    {
        public TransactionList(
            IEnumerable<Transaction> transactions,
            string groupBy,
            IEnumerable<string> tickers)
        {
            this.Transactions = Ordered(transactions, groupBy);
            this.Tickers = tickers;
            
            if (groupBy != null)
            {
                this.Grouped = Ordered(transactions, groupBy)
                    .GroupBy(t => GroupByValue(groupBy, t))
                    .Select(g => new {
                        name = g.Key,
                        transactions = new TransactionList(g, null, null)
                    });
            }
        }

        private IEnumerable<Transaction> Ordered(IEnumerable<Transaction> transactions, string groupBy)
        {
            if (groupBy == "ticker")
            {
                return transactions.OrderBy(t => t.Ticker);
            }

            return transactions.OrderByDescending(t => t.Date);
        }

        private static string GroupByValue(string groupBy, Transaction t)
        {
            if (groupBy == "ticker")
            {
                return t.Ticker;
            }
            return t.Date.ToString("MMMM, yyyy");
        }

        public IEnumerable<Transaction> Transactions { get; }
        public IEnumerable<string> Tickers { get; }
        public IEnumerable<object> Grouped { get; } 
        
        public double Credit => Transactions.Sum(t => t.Credit);
        public double Debit => Transactions.Sum(t => t.Debit);
    }
}