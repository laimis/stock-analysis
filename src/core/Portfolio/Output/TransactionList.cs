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
                    .GroupBy(t => t.Date.ToString("yyyy-MM-01"))
                    .OrderByDescending(g => g.Key)
                    .Select(g => new {
                        name = g.Key,
                        transactions = new TransactionList(g, false)
                    });
            }
        }

        public IEnumerable<Transaction> Transactions { get; }

        public IEnumerable<object> Grouped { get; } 
        
        public double Profit => Transactions.Sum(t => t.Profit);
    }
}