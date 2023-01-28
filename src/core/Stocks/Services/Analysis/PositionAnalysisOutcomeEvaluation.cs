using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Brokerage;

namespace core.Stocks.Services.Analysis
{
    public class PositionAnalysisOutcomeEvaluation
    {
        private const decimal PercentToStopThreshold = -0.02m;
        private const decimal PercentToStopNotSetValue = -1m;
        
        internal static IEnumerable<AnalysisOutcomeEvaluation> Evaluate(List<TickerOutcomes> tickerOutcomes, IEnumerable<Order> orders)
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

            // positions whose gain difference is greater than 10 should be highlighted positevely
            yield return new AnalysisOutcomeEvaluation(
                "Positions with much greater gain than drawdown",
                OutcomeType.Positive,
                PortfolioAnalysisKeys.GainAndDrawdownDiff,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.GainAndDrawdownDiff && o.value >= 0.1m))
                    .ToList()
            );

            // positions whose gain difference is less than -10 should be highlighted negatively
            yield return new AnalysisOutcomeEvaluation(
                "Positions with much greater drawdown than gain",
                OutcomeType.Negative,
                PortfolioAnalysisKeys.GainAndDrawdownDiff,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.GainAndDrawdownDiff && o.value <= -0.1m))
                    .ToList()
            );

            // positions whose gain difference in the last 10 bars is greater than 10
            // should be highlighted positevely
            yield return new AnalysisOutcomeEvaluation(
                "Positions with much greater gain than drawdown in last 10 bars",
                OutcomeType.Positive,
                PortfolioAnalysisKeys.GainDiffLast10,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.GainDiffLast10 && o.value >= 0.1m))
                    .ToList()
            );

            // positions whose gain difference in the last 10 bars is less than -10
            // should be highlighted negatively

            yield return new AnalysisOutcomeEvaluation(
                "Positions with much greater drawdown than gain in last 10 bars",
                OutcomeType.Negative,
                PortfolioAnalysisKeys.GainDiffLast10,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.GainDiffLast10 && o.value <= -0.1m))
                    .ToList()
            );

        }
    }
}