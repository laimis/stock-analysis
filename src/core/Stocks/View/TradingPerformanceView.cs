using System;

namespace core.Stocks
{
    public struct TradingPerformanceView
    {
        public static TradingPerformanceView Create(Span<PositionInstance> closedPositions)
        {
            if (closedPositions.Length == 0)
            {
                return new TradingPerformanceView();
            }

            var wins = 0;
            var maxWinAmount = 0m;
            var winMaxReturnPct = 0m;
            var total = closedPositions.Length;
            var totalWinAmount = 0m;
            var totalWinReturnPct = 0m;
            var totalWinDaysHeld = 0;

            var losses = 0;
            var maxLossAmount = 0m;
            var lossMaxReturnPct = 0m;
            var totalLossAmount = 0m;
            var totalLossReturnPct = 0m;
            var totalLossDaysHeld = 0;

            var totalDaysHeld = 0;
            var totalCost = 0m;
            var profit = 0m;
            var rrSum = 0m;
            var rrSumWeighted = 0m;
            
            foreach(var e in closedPositions)
            {
                totalDaysHeld += e.DaysHeld;
                profit += e.Profit;
                totalCost += e.Cost;
                rrSum += e.RR;
                rrSumWeighted += e.RRWeighted;

                if (e.Profit >= 0)
                {
                    wins++;
                    totalWinAmount += e.Profit;
                    maxWinAmount = Math.Max(maxWinAmount, e.Profit);
                    totalWinReturnPct += e.GainPct;
                    winMaxReturnPct = Math.Max(winMaxReturnPct, e.GainPct);
                    totalWinDaysHeld += e.DaysHeld;
                }
                else
                {
                    losses++;
                    totalLossAmount += Math.Abs(e.Profit);
                    maxLossAmount = Math.Max(maxLossAmount, Math.Abs(e.Profit));
                    totalLossReturnPct += Math.Abs(e.GainPct);
                    lossMaxReturnPct = Math.Max(lossMaxReturnPct, Math.Abs(e.GainPct));
                    totalLossDaysHeld += e.DaysHeld;
                }
            }
            
            var winningPct = wins * 1.0m / total;

            var adjustedWinningAmount = wins > 0 ? winningPct * totalWinAmount / wins : 0m;
            var adjustedLossingAmount = losses > 0 ? (1 - winningPct) * totalLossAmount / losses : 1000m;

            return new TradingPerformanceView {
                AvgDaysHeld = totalDaysHeld / total,
                AvgLossAmount = losses > 0 ? totalLossAmount / losses : 0,
                AvgReturnPct = totalCost > 0 ? profit / totalCost : 0,
                AvgWinAmount = wins > 0 ? totalWinAmount / wins : 0,
                EV = adjustedWinningAmount - adjustedLossingAmount,
                MaxLossAmount = maxLossAmount,
                LossAvgDaysHeld = losses > 0 ? totalLossDaysHeld / losses : 0,
                LossMaxReturnPct = lossMaxReturnPct,
                LossAvgReturnPct = totalLossReturnPct > 0 ? totalLossReturnPct / losses : 0,
                Losses = losses,
                MaxWinAmount = maxWinAmount,
                Profit = profit,
                rrSum = rrSum,
                rrSumWeighted = rrSumWeighted,
                Total = total,
                WinAvgDaysHeld = wins > 0 ? totalWinDaysHeld / wins : 0,
                WinAvgReturnPct = wins > 0 ? totalWinReturnPct / wins : 0,
                WinMaxReturnPct = winMaxReturnPct,
                WinPct = winningPct,
                Wins = wins
            };
        }

        public int Total { get; set; }
        public int Wins { get; set; }
        public decimal AvgWinAmount { get; set; }
        public decimal MaxWinAmount { get; set; }
        public decimal Profit { get; set; }
        public decimal WinAvgReturnPct { get; set; }
        public decimal WinMaxReturnPct { get; set; }
        public double WinAvgDaysHeld { get; set; }
        public int Losses { get; set; }
        public decimal AvgLossAmount { get; set; }
        public decimal MaxLossAmount { get; set; }
        public decimal LossAvgReturnPct { get; set; }
        public decimal LossMaxReturnPct { get; set; }
        public double LossAvgDaysHeld { get; set; }
        public decimal WinPct { get; set; }
        public decimal EV { get; set; }
        public decimal AvgReturnPct { get; set; }
        public double AvgDaysHeld { get; set; }
        public decimal rrSum { get; set; }
        public decimal rrSumWeighted { get; set; }
        public decimal ReturnPctRatio => LossAvgReturnPct switch {
            0m => 0m,
            _ => WinAvgReturnPct / LossAvgReturnPct
        };

        public decimal ProfitRatio => AvgLossAmount switch {
            0m => 0m,
            _ => AvgWinAmount / AvgLossAmount
        };
    }
}