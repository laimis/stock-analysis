using System.Collections.Generic;
using System.Linq;

namespace core.Stocks.Services.Analysis
{
    public class PositionAnalysisOutcomeEvaluation
    {
        private const decimal PercentToStopThreshold = -0.02m;
        private const decimal RecentlyOpenThreshold = 5;
        private const decimal WithinTwoWeeksThreshold = 14;
        
        internal static IEnumerable<AnalysisOutcomeEvaluation> Evaluate(List<TickerOutcomes> tickerOutcomes)
        {
            // stocks that are below stop loss
            yield return new AnalysisOutcomeEvaluation(
                "Below stop loss",
                OutcomeType.Negative,
                PortfolioAnalysisKeys.PercentToStopLoss,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.PercentToStopLoss && o.value > 0m))
                    .ToList()
            );

            // stocks whose stops are close
            yield return new AnalysisOutcomeEvaluation(
                "Stop loss at risk",
                OutcomeType.Neutral,
                PortfolioAnalysisKeys.PercentToStopLoss,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.PercentToStopLoss && o.value >= PercentToStopThreshold && o.value <= 0m))
                    .ToList()
            );

            // stocks that have been recently open
            yield return new AnalysisOutcomeEvaluation(
                $"Opened in the last {RecentlyOpenThreshold} days",
                OutcomeType.Neutral,
                PortfolioAnalysisKeys.DaysSinceOpened,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.DaysSinceOpened && o.value <= RecentlyOpenThreshold))
                    .ToList()
            );

            // stocks that have been opened within two weeks but not recently
            yield return new AnalysisOutcomeEvaluation(
                $"Opened in the last {WithinTwoWeeksThreshold} days",
                OutcomeType.Neutral,
                PortfolioAnalysisKeys.DaysSinceOpened,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.DaysSinceOpened && o.value <= WithinTwoWeeksThreshold))
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
        }
    }
}