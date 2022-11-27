#nullable enable
using System;
using System.Collections.Generic;

namespace core.Stocks.Services.Analysis
{
    public class PositionAnalysis
    {
        public static IEnumerable<AnalysisOutcome> Generate(PositionInstance position)
        {
            // add price as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.Price,
                OutcomeType.Neutral,
                position.Price!.Value,
                $"Price: {position.Price.Value:C2}"
            );

            // add average cost per share as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.AverageCost,
                OutcomeType.Neutral,
                Math.Round(position.AverageCostPerShare, 2),
                $"Average cost per share is {position.AverageCostPerShare:C2}");

            // add number of shares as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.NumberOfShares,
                OutcomeType.Neutral,
                position.NumberOfShares,
                $"Number of shares: {position.NumberOfShares}"
            );

            // gain in position
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.GainPct,
                position.UnrealizedGainPct >= 0 ? OutcomeType.Positive : OutcomeType.Negative,
                Math.Round( (position.UnrealizedGainPct ?? 0) * 100, 2),
                $"{position.UnrealizedGainPct:P}"
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
                $"{position.RR:N2}"
            );
            
            var stopLoss = Math.Round(position.StopPrice ?? 0 * 100, 2);

            // add stop loss as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.StopLoss,
                OutcomeType.Neutral,
                stopLoss,
                $"Stop loss is {stopLoss:C2}");

            var pctToStop = Math.Round( (position.PercentToStop ?? -1) * 100, 2);
                
            yield return new AnalysisOutcome(
                    PortfolioAnalysisKeys.PercentToStopLoss,
                    pctToStop < 0 ? OutcomeType.Positive : OutcomeType.Negative,
                    pctToStop,
                    $"% difference to stop loss {stopLoss} is {pctToStop}"
                );

            // add risk amount as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.RiskAmount,
                OutcomeType.Neutral,
                position.RiskedAmount ?? 0,
                $"Risk amount is {position.RiskedAmount:C2}");

            // add last transaction age
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.DaysSinceLastTransaction,
                OutcomeType.Neutral,
                position.DaysSinceLastTransaction,
                $"Last transaction was {position.DaysSinceLastTransaction} days ago"
            );
            
            // achieved r1
            var r1 = position.GetRRLevel(0);
            var r1Achieved = position.Price >= r1;
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.R1Achieved,
                r1Achieved ? OutcomeType.Positive : OutcomeType.Neutral,
                r1Achieved ? 1 : 0,
                $"R1 achieved: {r1}"
            );

            // achieved r2
            var r2 = position.GetRRLevel(1);
            var r2Achieved = position.Price >= r2;
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.R2Achieved,
                r2Achieved ? OutcomeType.Positive : OutcomeType.Neutral,
                r2Achieved ? 1 : 0,
                $"R2 achieved: {r2}"
            );

            // achieved r3
            var r3 = position.GetRRLevel(2);
            var r3Achieved = position.Price >= r3;
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.R3Achieved,
                r3Achieved ? OutcomeType.Positive : OutcomeType.Neutral,
                r3Achieved ? 1 : 0,
                $"R3 achieved: {r3}"
            );

            // achieved r4
            var r4 = position.GetRRLevel(3);
            var r4Achieved = position.Price >= r4;
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.R4Achieved,
                r4Achieved ? OutcomeType.Positive : OutcomeType.Neutral,
                r4Achieved ? 1 : 0,
                $"R4 achieved: {r4}"
            );
        }

    }

    internal class PortfolioAnalysisKeys
    {
        public static string PercentToStopLoss = "PercentToStopLoss";
        public static string R1Achieved = "R1Achieved";
        public static string R2Achieved = "R2Achieved";
        public static string R3Achieved = "R3Achieved";
        public static string R4Achieved = "R4Achieved";
        public static string GainPct = "GainPct";
        public static string AverageCost = "AverageCost";
        public static string StopLoss = "StopLoss";
        public static string RR = "RR";
        public static string Price = "Price";
        public static string NumberOfShares = "NumberOfShares";
        public static string RiskAmount = "RiskedAmount";
        public static string DaysSinceLastTransaction = "DaysSinceLastTransaction";
    }
}
#nullable restore