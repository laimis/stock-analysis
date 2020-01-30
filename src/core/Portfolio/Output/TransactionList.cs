using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Portfolio.Output
{
    public class TransactionList
    {
        public TransactionList(IEnumerable<Transaction> transactions, bool grouped)
        {
            this.Transactions = transactions;
            
            if (grouped)
            {
                this.Grouped = this.Transactions
                    .OrderByDescending(t => t.Date)
                    .GroupBy(t => t.Date.ToString("yyyy-MM-01"))
                    .Select(g => new {
                        name = g.Key,
                        transactions = new TransactionList(g, false)
                    });
            }
        }

        public IEnumerable<Transaction> Transactions { get; }
        public IEnumerable<object> Grouped { get; } 
        
        public double Credit => Transactions.Sum(t => t.Credit);
    }
}