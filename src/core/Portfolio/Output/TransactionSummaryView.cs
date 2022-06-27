using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Portfolio.Output
{
    public class TransactionSummaryView
    {
        public TransactionSummaryView(
            DateTimeOffset start,
            DateTimeOffset end,
            List<Transaction> stockTransactions,
            List<Transaction> optionTransactions,
            List<Transaction> plStockTransactions,
            List<Transaction> plOptionTransactions)
        {
            Start = start;
            End = end;
            StockTransactions = stockTransactions;
            OptionTransactions = optionTransactions;
            PLStockTransactions = plStockTransactions;
            PLOptionTransactions = plOptionTransactions;

            StockProfit = plStockTransactions.Select(t => t.Profit).Sum();
            OptionProfit = plOptionTransactions.Select(t => t.Profit).Sum();
        }

        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
        public List<Transaction> StockTransactions { get; }
        public List<Transaction> OptionTransactions { get; }
        public List<Transaction> PLStockTransactions { get; }
        public List<Transaction> PLOptionTransactions { get; }
        public decimal StockProfit { get; }
        public decimal OptionProfit { get; }
    }
}