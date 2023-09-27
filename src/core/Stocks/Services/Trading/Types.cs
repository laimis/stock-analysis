using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services.Analysis;

// complains about lowercase property names, doing that due to json formatting
#pragma warning disable IDE1006


namespace core.Stocks.Services.Trading
{
    public static class TradingStrategyConstants
    {
        // TODO: this needs to come from the environment or user settings
        public const decimal AvgPercentGain = 0.07m;
        public const decimal DefaultStopPriceMultiplier = 0.95m;
        public const int MaxNumberOfDaysToSimulate = 365;
        public const string ActualTradesName = "Actual trades ⭐";
    }

    public interface ITradingStrategy
    {
        TradingStrategyResult Run(PositionInstance position, IEnumerable<PriceBar> bars);
    }

    public class TradingStrategyResults
    {
        public List<TradingStrategyResult> Results { get; } = new List<TradingStrategyResult>();

        internal void Add(TradingStrategyResult result) => Results.Add(result);

        internal void Insert(int index, TradingStrategyResult result) =>
            Results.Insert(index, result);

        public string FailedReason { get; private set; }
        public bool Failed => FailedReason != null;

        public void MarkAsFailed(string reason) => FailedReason = reason;
    }

    public struct TradingPerformance
    {
        public static TradingPerformance Create(IEnumerable<PositionInstance> closedPositions)
        {
            var wins = 0;
            var maxWinAmount = 0m;
            var winMaxReturnPct = 0m;
            var numberOfTrades = 0;
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
            var earliestDate = DateTimeOffset.MaxValue;
            var latestDate = DateTimeOffset.MinValue;
            var gradeDistribution = new SortedDictionary<TradeGrade, int>();

            var totalRRWins = 0m;
            var totalRRLosses = 0m;

            foreach(var e in closedPositions)
            {
                numberOfTrades++;
                totalDaysHeld += e.DaysHeld;
                profit += e.Profit;
                totalCost += e.Cost != 0 ? e.Cost : e.CompletedPositionCost;
                rrSum += e.RR;
                earliestDate = e.Opened < earliestDate ? e.Opened : earliestDate;
                latestDate = e.Closed.HasValue && e.Closed.Value > latestDate ? e.Closed.Value : latestDate;

                if (e.Profit >= 0)
                {
                    wins++;
                    totalWinAmount += e.Profit;
                    maxWinAmount = Math.Max(maxWinAmount, e.Profit);
                    totalWinReturnPct += e.GainPct;
                    winMaxReturnPct = Math.Max(winMaxReturnPct, e.GainPct);
                    totalWinDaysHeld += e.DaysHeld;
                    totalRRWins += e.RR;
                }
                else
                {
                    losses++;
                    totalLossAmount += Math.Abs(e.Profit);
                    maxLossAmount = Math.Max(maxLossAmount, Math.Abs(e.Profit));
                    totalLossReturnPct += Math.Abs(e.GainPct);
                    lossMaxReturnPct = Math.Max(lossMaxReturnPct, Math.Abs(e.GainPct));
                    totalLossDaysHeld += e.DaysHeld;
                    totalRRLosses += Math.Abs(e.RR);
                }
                
                if (e.Grade != null)
                {
                    if (gradeDistribution.ContainsKey(e.Grade))
                    {
                        gradeDistribution[e.Grade]++;
                    }
                    else
                    {
                        gradeDistribution.Add(e.Grade, 1);
                    }
                }
            }

            if (numberOfTrades == 0)
            {
                return new TradingPerformance();
            }
            
            var winningPct = wins * 1.0m / numberOfTrades;

            var adjustedWinningAmount = wins > 0 ? winningPct * totalWinAmount / wins : 0m;
            var adjustedLosingAmount = losses > 0 ? (1 - winningPct) * totalLossAmount / losses : 0m;

            var rrRatio = (totalRRWins > 0 && totalRRLosses > 0) switch {
                true => totalRRWins / wins / (totalRRLosses / losses),
                false => 0m
            };
            
            return new TradingPerformance {
                AvgDaysHeld = totalDaysHeld / (numberOfTrades * 1.0),
                AvgLossAmount = losses > 0 ? totalLossAmount / losses : 0,
                AvgReturnPct = totalCost > 0 ? profit / totalCost : 0,
                AvgWinAmount = wins > 0 ? totalWinAmount / wins : 0,
                EV = adjustedWinningAmount - adjustedLosingAmount,
                EarliestDate = earliestDate,
                GradeDistribution = gradeDistribution.Select(kp => new LabelWithFrequency(label: kp.Key.Value, frequency: kp.Value)).ToArray(),
                LatestDate = latestDate,
                MaxLossAmount = maxLossAmount,
                LossAvgDaysHeld = losses > 0 ? totalLossDaysHeld / (losses * 1.0) : 0,
                LossMaxReturnPct = lossMaxReturnPct,
                LossAvgReturnPct = losses > 0  ? totalLossReturnPct / losses : 0,
                Losses = losses,
                MaxWinAmount = maxWinAmount,
                Profit = profit,
                rrSum = rrSum,
                rrRatio = rrRatio,
                NumberOfTrades = numberOfTrades,
                TotalCost = totalCost,
                WinAvgDaysHeld = wins > 0 ? totalWinDaysHeld / (wins * 1.0) : 0,
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
        public decimal rrRatio { get; set; }
        public readonly decimal ReturnPctRatio => LossAvgReturnPct switch {
            0m => 0m,
            _ => WinAvgReturnPct / LossAvgReturnPct
        };

        public readonly decimal ProfitRatio => AvgLossAmount switch {
            0m => 0m,
            _ => AvgWinAmount / AvgLossAmount
        };

        public DateTimeOffset EarliestDate { get; private set; }
        public DateTimeOffset LatestDate { get; private set; }
        public LabelWithFrequency[] GradeDistribution { get; private set; }
        public decimal TotalCost { get; private set; }
    }

    public record struct TradingStrategyPerformance(
        string strategyName,
        TradingPerformance performance,
        PositionInstance[] positions
    ) {
        public readonly int NumberOfOpenPositions => positions.Count(p => p.Closed == null);
    }

    public record struct TradingStrategyResult(
        decimal maxDrawdownPct,
        decimal maxGainPct,
        decimal maxDrawdownPctRecent,
        decimal maxGainPctRecent,
        PositionInstance position,
        string strategyName
    );

    internal record struct SimulationContext(
        PositionInstance Position,
        decimal MaxGain,
        decimal MaxDrawdown,
        List<PriceBar> Last10Bars
    );
}

#pragma warning restore IDE1006