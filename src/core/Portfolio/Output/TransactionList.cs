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
            Transactions = Ordered(transactions, groupBy);
            Tickers = tickers;
            
            if (groupBy != null)
            {
                Grouped = Ordered(transactions, groupBy)
                    .GroupBy(t => GroupByValue(groupBy, t))
                    .Select(g => new TransactionGroup(
                        g.Key,
                        new TransactionList(g, null, null)
                    ));

                if (groupBy == "ticker")
                {
                    Grouped = Grouped.OrderByDescending(a => a.Transactions.Credit - a.Transactions.Debit);
                }
            }
        }

        private IEnumerable<Transaction> Ordered(IEnumerable<Transaction> transactions, string groupBy)
        {
            if (groupBy == "ticker")
            {
                return transactions.OrderBy(t => t.Ticker);
            }

            return transactions.OrderByDescending(t => t.DateAsDate);
        }

        private static string GroupByValue(string groupBy, Transaction t)
        {
            if (groupBy == "ticker")
            {
                return t.Ticker;
            }

            if (groupBy == "week")
            {
                var mon = t.DateAsDate.AddDays(-(int)t.DateAsDate.DayOfWeek+1);
                return mon.ToString("MMMM dd, yyyy");
            }

            return t.DateAsDate.ToString("MMMM, yyyy");
        }

        public IEnumerable<Transaction> Transactions { get; set; }
        public IEnumerable<string> Tickers { get; set; }
        public IEnumerable<TransactionGroup> Grouped { get; set; } 
        
        public decimal Credit => Transactions.Sum(t => t.Amount);
        public decimal Debit => Transactions.Sum(t => t.Amount);
    }
}