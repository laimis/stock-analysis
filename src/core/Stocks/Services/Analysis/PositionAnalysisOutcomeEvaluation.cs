using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.SEC;

namespace core.Stocks.Services.Analysis
{
    public class PositionAnalysisOutcomeEvaluation
    {
        private const decimal PercentToStopThreshold = -0.02m;
        private const decimal RecentlyOpenThreshold = 5;
        
        internal static IEnumerable<AnalysisOutcomeEvaluation> Evaluate(
            List<TickerOutcomes> tickerOutcomes,
            Dictionary<string, CompanyFilings> filings)
        {
            // stocks whose stops are close
            yield return new AnalysisOutcomeEvaluation(
                "Stop loss at risk",
                OutcomeType.Negative,
                PortfolioAnalysisKeys.PercentToStopLoss,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.PercentToStopLoss && o.value >= PercentToStopThreshold))
                    .ToList()
            );

            // stocks that have been recently open
            yield return new AnalysisOutcomeEvaluation(
                "Recently Opened",
                OutcomeType.Neutral,
                PortfolioAnalysisKeys.DaysSinceOpened,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.DaysSinceOpened && o.value <= RecentlyOpenThreshold))
                    .ToList()
            );

            // positions that don't have strategy label assigned
            yield return new AnalysisOutcomeEvaluation(
                "No Strategy",
                OutcomeType.Negative,
                PortfolioAnalysisKeys.StrategyLabel,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.StrategyLabel && o.value == 0))
                    .ToList()
            );

            // TODO: still thinking about it if I want to do this...
            // // positions that don't have sell orders
            // yield return new AnalysisOutcomeEvaluation(
            //     "No Sell Orders",
            //     OutcomeType.Neutral,
            //     PortfolioAnalysisKeys.HasSellOrder,
            //     tickerOutcomes
            //         .Where(t =>
            //             t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.HasSellOrder && o.value == 0))
            //         .ToList()
            // );
        }
    }
}