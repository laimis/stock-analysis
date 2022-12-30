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
                OutcomeValueType.Currency,
                $"Price: {position.Price.Value:C2}"
            );

            // add average cost per share as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.AverageCost,
                OutcomeType.Neutral,
                Math.Round(position.AverageCostPerShare, 2),
                OutcomeValueType.Currency,
                $"Average cost per share is {position.AverageCostPerShare:C2}");

            // add number of shares as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.NumberOfShares,
                OutcomeType.Neutral,
                position.NumberOfShares,
                OutcomeValueType.Number,
                $"Number of shares: {position.NumberOfShares}"
            );

            // gain in position
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.GainPct,
                position.UnrealizedGainPct >= 0 ? OutcomeType.Positive : OutcomeType.Negative,
                (position.UnrealizedGainPct ?? 0),
                OutcomeValueType.Percentage,
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
                OutcomeValueType.Number,
                $"{position.RR:N2}"
            );

            // profit in position
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.Profit,
                position.CombinedProfit >= 0 ? OutcomeType.Positive : OutcomeType.Negative,
                position.CombinedProfit,
                OutcomeValueType.Currency,
                $"{position.CombinedProfit}"
            );
            
            var stopLoss = position.StopPrice ?? 0;

            // add stop loss as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.StopLoss,
                OutcomeType.Neutral,
                stopLoss,
                OutcomeValueType.Currency,
                $"Stop loss is {stopLoss:C2}");

            var pctToStop = (position.PercentToStop ?? -1);
                
            yield return new AnalysisOutcome(
                    PortfolioAnalysisKeys.PercentToStopLoss,
                    pctToStop < 0 ? OutcomeType.Positive : OutcomeType.Negative,
                    pctToStop,
                    OutcomeValueType.Percentage,
                    $"% difference to stop loss {stopLoss} is {pctToStop}"
                );

            // add risk amount as outcome
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.RiskAmount,
                OutcomeType.Neutral,
                position.RiskedAmount ?? 0,
                OutcomeValueType.Currency,
                $"Risk amount is {position.RiskedAmount:C2}");

            // add last transaction age
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.DaysSinceLastTransaction,
                OutcomeType.Neutral,
                position.DaysSinceLastTransaction,
                OutcomeValueType.Number,
                $"Last transaction was {position.DaysSinceLastTransaction} days ago"
            );
        }

    }

    internal class PortfolioAnalysisKeys
    {
        public static string PercentToStopLoss = "PercentToStopLoss";
        public static string GainPct = "GainPct";
        public static string AverageCost = "AverageCost";
        public static string StopLoss = "StopLoss";
        public static string RR = "RR";
        public static string Profit = "Profit";
        public static string Price = "Price";
        public static string NumberOfShares = "NumberOfShares";
        public static string RiskAmount = "RiskedAmount";
        public static string DaysSinceLastTransaction = "DaysSinceLastTransaction";
    }
}
#nullable restore