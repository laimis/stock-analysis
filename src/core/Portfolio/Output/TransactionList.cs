using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Portfolio.Output
{
    public class TransactionList
    {
        public TransactionList(IEnumerable<Transaction> transactions) => this.Transactions = transactions;

        public IEnumerable<Transaction> Transactions { get; }

        public double Profit => Transactions.Sum(t => t.Value);
    }
}