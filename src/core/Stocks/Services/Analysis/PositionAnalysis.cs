#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Stocks.Services.Analysis
{
    public class PositionAnalysis
    {
        public static IEnumerable<AnalysisOutcome> Generate(
            PositionInstance position,
            Shared.Adapters.Stocks.PriceBar[] bars)
        {
            // add price as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.Price,
                OutcomeType.Neutral,
                position.Price!.Value,
                Shared.ValueFormat.Currency,
                $"Price: {position.Price.Value:C2}"
            );

            var stopLoss = position.StopPrice ?? 0;

            // add stop loss as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.StopLoss,
                OutcomeType.Neutral,
                stopLoss,
                Shared.ValueFormat.Currency,
                $"Stop loss is {stopLoss:C2}");

            var pctToStop = (position.PercentToStop ?? -1);
                
            yield return new AnalysisOutcome(
                    PortfolioAnalysisKeys.PercentToStopLoss,
                    pctToStop < 0 ? OutcomeType.Positive : OutcomeType.Negative,
                    pctToStop,
                    Shared.ValueFormat.Percentage,
                    $"% difference to stop loss {stopLoss} is {pctToStop}"
                );

            // add average cost per share as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.AverageCost,
                OutcomeType.Neutral,
                Math.Round(position.AverageCostPerShare, 2),
                Shared.ValueFormat.Currency,
                $"Average cost per share is {position.AverageCostPerShare:C2}");

            // gain in position
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.GainPct,
                position.GainPct >= 0 ? OutcomeType.Positive : OutcomeType.Negative,
                position.GainPct,
                Shared.ValueFormat.Percentage,
                $"{position.GainPct:P}"
            );

            var rrOutcomeType = position.RR switch {
                >= 1 => OutcomeType.Positive,
                < 0 => OutcomeType.Negative,
                _ => OutcomeType.Neutral
            };

            // rr in position
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.RR,
                rrOutcomeType,
                Math.Round(position.RR, 2),
                Shared.ValueFormat.Number,
                $"{position.RR:N2}"
            );

            // profit in position
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.Profit,
                position.CombinedProfit >= 0 ? OutcomeType.Positive : OutcomeType.Negative,
                position.CombinedProfit,
                Shared.ValueFormat.Currency,
                $"{position.CombinedProfit}"
            );

            // add max gain from bars as outcome
            var max = bars.Max(b => b.High);
            var gain = (max - position.CompletedPositionCostPerShare)/position.CompletedPositionCostPerShare;

            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.MaxGain,
                OutcomeType.Neutral,
                gain,
                Shared.ValueFormat.Percentage,
                $"Max gain is {gain:P}");

            // add max drawdown from bars as outcome
            var min = bars.Min(b => b.Low);
            var drawdown = (min - position.CompletedPositionCostPerShare)/position.CompletedPositionCostPerShare;
            
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.MaxDrawdown,
                OutcomeType.Neutral,
                drawdown,
                Shared.ValueFormat.Percentage,
                $"Max drawdown is {drawdown:P}");

            // difference between max gain and max drawdown
            var maxGainDrawdownDiff = gain - drawdown * -1;

            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.GainAndDrawdownDiff,
                maxGainDrawdownDiff >= 0 ? OutcomeType.Positive : OutcomeType.Negative,
                maxGainDrawdownDiff,
                Shared.ValueFormat.Percentage,
                $"Max gain drawdown diff is {maxGainDrawdownDiff:P}");

            // add max gain in the last 10 bars as outcome
            var last10 = bars.TakeLast(10).ToArray();
            var last10Max = last10.Max(b => b.High);
            var last10Gain = (last10Max - last10[0].Close)/last10[0].Close;

            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.MaxGainLast10,
                OutcomeType.Neutral,
                last10Gain,
                Shared.ValueFormat.Percentage,
                $"Max gain in last 10 bars is {last10Gain:P}");

            // add max drawdown in the last 10 bars as outcome
            var last10Min = last10.Min(b => b.Low);
            var last10Drawdown = (last10Min - last10[0].Close)/last10[0].Close;

            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.MaxDrawdownLast10,
                OutcomeType.Neutral,
                last10Drawdown,
                Shared.ValueFormat.Percentage,
                $"Max drawdown in last 10 bars is {last10Drawdown:P}");

            // difference between max gain and max drawdown
            var last10MaxGainDrawdownDiff = last10Gain - last10Drawdown * -1;

            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.GainDiffLast10,
                last10MaxGainDrawdownDiff >= 0 ? OutcomeType.Positive : OutcomeType.Negative,
                last10MaxGainDrawdownDiff,
                Shared.ValueFormat.Percentage,
                $"Max gain drawdown diff in last 10 bars is {last10MaxGainDrawdownDiff:P}");

            // add risk amount as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.RiskAmount,
                OutcomeType.Neutral,
                position.RiskedAmount ?? 0,
                Shared.ValueFormat.Currency,
                $"Risk amount is {position.RiskedAmount:C2}");

            // add days held
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.DaysHeld,
                OutcomeType.Neutral,
                position.DaysHeld,
                Shared.ValueFormat.Number,
                $"Days held: {position.DaysHeld}"
            );

            // add last transaction age
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.DaysSinceLastTransaction,
                OutcomeType.Neutral,
                position.DaysSinceLastTransaction,
                Shared.ValueFormat.Number,
                $"Last transaction was {position.DaysSinceLastTransaction} days ago"
            );

            // add positin size as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.PositionSize,
                OutcomeType.Neutral,
                position.Cost,
                Shared.ValueFormat.Currency,
                $"Position size is {position.Cost}"
            );

            // add days since opened as outcome
            var daysSinceOpened =
                Math.Ceiling(
                    DateTimeOffset.UtcNow.Subtract(position.Opened!.Value).TotalDays
                );

            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.DaysSinceOpened,
                OutcomeType.Neutral,
                (decimal) daysSinceOpened,
                Shared.ValueFormat.Number,
                $"Days since opened: {daysSinceOpened}"
            );

            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.StrategyLabel,
                OutcomeType.Negative,
                position.ContainsLabel("strategy") ? 1 : 0,
                Shared.ValueFormat.Boolean,
                $"Missing strategy label"
            );
        }

    }

    internal class PortfolioAnalysisKeys
    {
        public static string PercentToStopLoss = "PercentToStopLoss";
        public static string DaysSinceOpened = "DaysSinceOpened";
        public static string RecentFilings = "RecentFilings";
        public static string GainPct = "GainPct";
        public static string AverageCost = "AverageCost";
        public static string StopLoss = "StopLoss";
        public static string RR = "RR";
        public static string Profit = "Profit";
        public static string Price = "Price";
        public static string RiskAmount = "RiskedAmount";
        public static string DaysSinceLastTransaction = "DaysSinceLastTransaction";
        public static string PositionSize = "PositionSize";
        public static string DaysHeld = "DaysHeld";
        public static string MaxDrawdown = "MaxDrawdown";
        public static string MaxGain = "MaxGain";
        public static string GainAndDrawdownDiff = "GainDiff";
        public static string MaxDrawdownLast10 = "MaxDrawdownLast10";
        public static string MaxGainLast10 = "MaxGainLast10";
        public static string GainDiffLast10 = "GainDiffLast10";
        public static string StrategyLabel = "StrategyLabel";
    }
}
#nullable restore