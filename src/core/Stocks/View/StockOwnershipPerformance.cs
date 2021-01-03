using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Stocks
{
    public class StockOwnershipPerformance
    {
        public StockOwnershipPerformance(){}
        
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
                this.AvgLossAmount = Math.Abs(losses.Average(t => t.Profit));
                this.MaxLossAmount = Math.Abs(losses.Min(t => t.Profit));
                this.LossAvgReturnPct = Math.Abs(losses.Average(t => t.ReturnPct));
            }
            
            this.WinPct = (1.0 * this.Wins) / this.Total;
            this.EV = WinPct * this.AvgWinAmount - (1-WinPct) * this.AvgLossAmount;
            this.AvgReturnPct = closedTransactions.Average(t => t.ReturnPct);
        }

        public int Total { get; set; }
        public int Wins { get; set; }
        public double AvgWinAmount { get; set; }
        public double MaxWinAmount { get; set; }
        public double WinAvgReturnPct { get; set; }
        public int Losses { get; set; }
        public double AvgLossAmount { get; set; }
        public double MaxLossAmount { get; set; }
        public double LossAvgReturnPct { get; set; }
        public double WinPct { get; set; }
        public double EV { get; set; }
        public double AvgReturnPct { get; set; }
    }
}