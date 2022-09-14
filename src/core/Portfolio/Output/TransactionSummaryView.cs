using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;
using core.Stocks;

namespace core.Portfolio.Output
{
    public class TransactionSummaryView
    {
        public TransactionSummaryView(
            DateTimeOffset start,
            DateTimeOffset end,
            List<PositionInstance> openPositions,
            List<PositionInstance> closedPositions,
            List<Transaction> stockTransactions,
            List<Transaction> optionTransactions,
            List<Transaction> plStockTransactions,
            List<Transaction> plOptionTransactions)
        {
            Start = start;
            End = end;
            OpenPositions = openPositions;
            ClosedPositions = closedPositions;
            StockTransactions = stockTransactions;
            OptionTransactions = optionTransactions;
            PLStockTransactions = plStockTransactions;
            PLOptionTransactions = plOptionTransactions;

            StockProfit = plStockTransactions.Select(t => t.Amount).Sum();
            OptionProfit = plOptionTransactions.Select(t => t.Amount).Sum();
        }

        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
        public List<PositionInstance> OpenPositions { get; }
        public List<PositionInstance> ClosedPositions { get; }
        public List<Transaction> StockTransactions { get; }
        public List<Transaction> OptionTransactions { get; }
        public List<Transaction> PLStockTransactions { get; }
        public List<Transaction> PLOptionTransactions { get; }
        public decimal StockProfit { get; }
        public decimal OptionProfit { get; }
    }
}