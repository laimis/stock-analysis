using System;

namespace core.Stocks
{
    public struct TradingPerformanceView
    {
        public static TradingPerformanceView Create(Span<PositionInstance> positions)
        {
            if (positions.Length == 0)
            {
                return new TradingPerformanceView();
            }

            var wins = 0;
            var maxWinAmount = 0m;
            var winMaxReturnPct = 0m;
            var numberOfTrades = positions.Length;
            var totalWinAmount = 0m;
            var totalWinReturnPct = 0m;
            var totalWinDaysHeld = 0;

            var losses = 0;
            var maxLossAmount = 0m;
            var lossMaxReturnPct = 0m;
            var totalLossAmount = 0m;
            var totalLossReturnPct = 0m;
            var totalLossDaysHeld = 0;
            var totalPercentReturn = 0m;

            var totalDaysHeld = 0;
            var profit = 0m;
            var rrSum = 0m;
            var rrSumWeighted = 0m;
            
            foreach(var e in positions)
            {
                totalDaysHeld += e.DaysHeld;
                profit += e.Profit + (e.UnrealizedProfit.HasValue ? e.UnrealizedProfit.Value : 0);
                rrSum += e.RR;
                totalPercentReturn += e.GainPct;
                rrSumWeighted += e.RRWeighted;

                if (profit >= 0)
                {
                    wins++;
                    totalWinAmount += profit;
                    maxWinAmount = Math.Max(maxWinAmount,profit);
                    totalWinReturnPct += e.GainPct;
                    winMaxReturnPct = Math.Max(winMaxReturnPct, e.GainPct);
                    totalWinDaysHeld += e.DaysHeld;
                }
                else
                {
                    losses++;
                    totalLossAmount += Math.Abs(profit);
                    maxLossAmount = Math.Max(maxLossAmount, Math.Abs(profit));
                    totalLossReturnPct += Math.Abs(e.GainPct);
                    lossMaxReturnPct = Math.Max(lossMaxReturnPct, Math.Abs(e.GainPct));
                    totalLossDaysHeld += e.DaysHeld;
                }
            }
            
            var winningPct = wins * 1.0m / numberOfTrades;

            var adjustedWinningAmount = wins > 0 ? winningPct * totalWinAmount / wins : 0m;
            var adjustedLossingAmount = losses > 0 ? (1 - winningPct) * totalLossAmount / losses : 1000m;

            return new TradingPerformanceView {
                AvgDaysHeld = totalDaysHeld / numberOfTrades,
                AvgLossAmount = losses > 0 ? totalLossAmount / losses : 0,
                AvgReturnPct = totalPercentReturn / numberOfTrades,
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
                NumberOfTrades = numberOfTrades,
                WinAvgDaysHeld = wins > 0 ? totalWinDaysHeld / wins : 0,
                WinAvgReturnPct = wins > 0 ? totalWinReturnPct / wins : 0,
                WinMaxReturnPct = winMaxReturnPct,
                WinPct = winningPct,
                Wins = wins
            };
        }

        public int NumberOfTrades { get; set; }
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