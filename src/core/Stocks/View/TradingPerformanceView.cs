using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Stocks
{
    public class TradingPerformanceView
    {
        public TradingPerformanceView(){}
        
        public TradingPerformanceView(List<PositionInstance> closedPositions)
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
                WinAvgReturnPct = wins.Average(t => t.ReturnPct);
                WinAvgDaysHeld = wins.Average(t => t.DaysHeld);
            }

            var losses = closedPositions.Where(t => t.Profit < 0).ToList();
            if (losses.Count > 0)
            {
                Losses = losses.Count;
                AvgLossAmount = Math.Abs(losses.Average(t => t.Profit));
                MaxLossAmount = Math.Abs(losses.Min(t => t.Profit));
                LossAvgReturnPct = Math.Abs(losses.Average(t => t.ReturnPct));
                LossAvgDaysHeld = losses.Average(t => t.DaysHeld);
            }
            
            WinPct = (1.0m * Wins) / Total;
            EV = WinPct * AvgWinAmount - (1-WinPct) * AvgLossAmount;
            AvgDaysHeld = closedPositions.Average(t => t.DaysHeld);

            var totalCost = closedPositions.Sum(t => t.Cost);
            var totalProfit = closedPositions.Sum(t => t.Profit);
            var totalReturnPct = totalCost > 0 ? totalProfit / totalCost : 0;
            AvgReturnPct = totalReturnPct;
        }

        public int Total { get; set; }
        public int Wins { get; set; }
        public decimal AvgWinAmount { get; set; }
        public decimal MaxWinAmount { get; set; }
        public decimal WinAvgReturnPct { get; set; }
        public double WinAvgDaysHeld { get; set; }
        public int Losses { get; set; }
        public decimal AvgLossAmount { get; set; }
        public decimal MaxLossAmount { get; set; }
        public decimal LossAvgReturnPct { get; set; }
        public double LossAvgDaysHeld { get; set; }
        public decimal WinPct { get; set; }
        public decimal EV { get; set; }
        public decimal AvgReturnPct { get; set; }
        public double AvgDaysHeld { get; set; }
    }
}