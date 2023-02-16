using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;
using core.Stocks;

namespace core.Portfolio.Views
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
    
    public class TransactionGroup
    {
        public TransactionGroup(string name, TransactionList transactions)
        {
            Name = name;
            Transactions = transactions;
        }

        public string Name { get; set; }
        public TransactionList Transactions { get; set; }
    }

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