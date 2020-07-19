using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Stocks
{
    internal class StockOwnershipPerformance
    {
        public StockOwnershipPerformance(List<Transaction> closedTransactions)
        {
            if (closedTransactions.Count == 0)
            {
                return;
            }

            this.Total = closedTransactions.Count;
            
            var wins = closedTransactions.Where(t => t.Profit >= 0).ToList();
            if (wins.Count > 0)
            {
                this.Wins = wins.Count;
                this.AvgWinAmount = wins.Average(t => t.Profit);
                this.MaxWinAmount = wins.Max(t => t.Profit);
                this.WinAvgReturnPct = wins.Average(t => t.ReturnPct);
            }

            var losses = closedTransactions.Where(t => t.Profit < 0).ToList();
            if (losses.Count > 0)
            {
                this.Losses = losses.Count;
                this.AvgLossAmount = losses.Average(t => t.Profit);
                this.MaxLossAmount = losses.Min(t => t.Profit);
                this.LossAvgReturnPct = losses.Average(t => t.ReturnPct);
            }
            
            this.WinPct = (1.0 * this.Wins) / this.Total;
            this.EV = WinPct * this.AvgWinAmount - (1-WinPct) * this.AvgLossAmount;
            this.AvgReturnPct = closedTransactions.Average(t => t.ReturnPct);
        }

        public int Total { get; }
        public int Wins { get; }
        public double AvgWinAmount { get; }
        public double MaxWinAmount { get; }
        public double WinAvgReturnPct { get; }
        public int Losses { get; }
        public double AvgLossAmount { get; }
        public double MaxLossAmount { get; }
        public double LossAvgReturnPct { get; }
        public double WinPct { get; }
        public double EV { get; }
        public double AvgReturnPct { get; }
    }
}