using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Stocks
{
    public class StockOwnershipPerformance
    {
        public StockOwnershipPerformance(){}
        
        public StockOwnershipPerformance(List<PositionInstance> closedPositions)
        {
            if (closedPositions.Count == 0)
            {
                return;
            }

            Total = closedPositions.Count;
            
            var wins = closedPositions.Where(t => t.Profit >= 0).ToList();
            if (wins.Count > 0)
            {
                Wins = wins.Count;
                AvgWinAmount = wins.Average(t => t.Profit);
                MaxWinAmount = wins.Max(t => t.Profit);
                WinAvgReturnPct = wins.Average(t => t.Percentage);
                WinAvgDaysHeld = wins.Average(t => t.DaysHeld);
            }

            var losses = closedPositions.Where(t => t.Profit < 0).ToList();
            if (losses.Count > 0)
            {
                Losses = losses.Count;
                AvgLossAmount = Math.Abs(losses.Average(t => t.Profit));
                MaxLossAmount = Math.Abs(losses.Min(t => t.Profit));
                LossAvgReturnPct = Math.Abs(losses.Average(t => t.Percentage));
                LossAvgDaysHeld = losses.Average(t => t.DaysHeld);
            }
            
            WinPct = (1.0 * Wins) / Total;
            EV = WinPct * AvgWinAmount - (1-WinPct) * AvgLossAmount;
            AvgReturnPct = closedPositions.Average(t => t.Percentage);
            AvgDaysHeld = closedPositions.Average(t => t.DaysHeld);
        }

        public int Total { get; set; }
        public int Wins { get; set; }
        public double AvgWinAmount { get; set; }
        public double MaxWinAmount { get; set; }
        public double WinAvgReturnPct { get; set; }
        public double WinAvgDaysHeld { get; set; }
        public int Losses { get; set; }
        public double AvgLossAmount { get; set; }
        public double MaxLossAmount { get; set; }
        public double LossAvgReturnPct { get; set; }
        public double LossAvgDaysHeld { get; set; }
        public double WinPct { get; set; }
        public double EV { get; set; }
        public double AvgReturnPct { get; set; }
        public double AvgDaysHeld { get; set; }
    }
}