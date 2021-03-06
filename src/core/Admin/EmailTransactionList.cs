using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Admin
{
    public class EmailTransactionList
    {
        public EmailTransactionList(
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
                    .Select(g => new EmailTransactionGroup(
                        g.Key,
                        new EmailTransactionList(g, null, null)
                    ));

                if (groupBy == "ticker")
                {
                    this.Grouped = this.Grouped.OrderByDescending(a => a.Transactions.Credit - a.Transactions.Debit);
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
        public IEnumerable<EmailTransactionGroup> Grouped { get; set; } 
        
        public double Credit => Transactions.Sum(t => t.Credit);
        public double Debit => Transactions.Sum(t => t.Debit);
    }
}