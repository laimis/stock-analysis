#nullable enable
using System;
using System.Collections.Generic;
using core.Shared.Adapters.Brokerage;

namespace core.Stocks.Services
{
    public class PositionAnalysis
    {
        public static IEnumerable<AnalysisOutcome> Generate(PositionInstance position, StockQuote quote)
        {
            // distance from stop loss to current price
            var currentPrice = Math.Max(quote.bidPrice, quote.lastPrice);
            
            var stopLoss = position.StopPrice ?? 0;
            
            var percentDiff = Math.Round((stopLoss - currentPrice) / currentPrice * 100, 2);
                
            yield return new AnalysisOutcome(
                    PortfolioAnalysisKeys.StopLossAtRisk,
                    percentDiff < 0 ? OutcomeType.Positive : OutcomeType.Negative,
                    percentDiff,
                    $"% difference to stop loss {stopLoss} is {percentDiff}"
                );
            
            // achieved r1
            var r1 = position.GetRRLevel(0);
            var r1Achieved = currentPrice >= r1;
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.R1Achieved,
                r1Achieved ? OutcomeType.Positive : OutcomeType.Neutral,
                r1Achieved ? 1 : 0,
                $"R1 achieved: {r1}"
            );

            // achieved r2
            var r2 = position.GetRRLevel(1);
            var r2Achieved = currentPrice >= r2;
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.R2Achieved,
                r2Achieved ? OutcomeType.Positive : OutcomeType.Neutral,
                r2Achieved ? 1 : 0,
                $"R2 achieved: {r2}"
            );

            // achieved r3
            var r3 = position.GetRRLevel(2);
            var r3Achieved = currentPrice >= r3;
            yield return new AnalysisOutcome(
                PortfolioAnalysisKeys.R3Achieved,
                r3Achieved ? OutcomeType.Positive : OutcomeType.Neutral,
                r3Achieved ? 1 : 0,
                $"R3 achieved: {r3}"
            );

            // achieved r4
            var r4 = position.GetRRLevel(3);
            var r4Achieved = currentPrice >= r4;
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
        public static string StopLossAtRisk = "StopLossAtRisk";
        public static string R1Achieved = "R1Achieved";
        public static string R2Achieved = "R2Achieved";
        public static string R3Achieved = "R3Achieved";
        public static string R4Achieved = "R4Achieved";
    }
}
#nullable restore