using System.Collections.Generic;
using System.Linq;

namespace core.Stocks.Services
{
    public class PositionAnalysisOutcomeEvaluation
    {
        private const decimal PercentToStopThreshold = -0.02m;
        
        internal static IEnumerable<OutcomeAnalysisEvaluation> Evaluate(List<TickerOutcomes> tickerOutcomes)
        {
            // stocks whose stops are close
                yield return new OutcomeAnalysisEvaluation(
                    "Stop loss at risk",
                    OutcomeType.Negative,
                    PortfolioAnalysisKeys.PercentToStopLoss,
                    tickerOutcomes
                        .Where(t =>
                            t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.PercentToStopLoss && o.value >= PercentToStopThreshold))
                        .ToList()
                );

                yield return new OutcomeAnalysisEvaluation(
                    "R1 achieved",
                    OutcomeType.Positive,
                    PortfolioAnalysisKeys.R1Achieved,
                    tickerOutcomes
                        .Where(t =>
                            t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.R1Achieved && o.value > 0) &&
                            !t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.R2Achieved && o.value > 0)
                        ).ToList()
                );

                yield return new OutcomeAnalysisEvaluation(
                    "R2 achieved",
                    OutcomeType.Positive,
                    PortfolioAnalysisKeys.R2Achieved,
                    tickerOutcomes
                        .Where(t =>
                            t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.R2Achieved && o.value > 0) &&
                            !t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.R3Achieved && o.value > 0)
                        ).ToList()
                );

                yield return new OutcomeAnalysisEvaluation(
                    "R3 achieved",
                    OutcomeType.Positive,
                    PortfolioAnalysisKeys.R3Achieved,
                    tickerOutcomes
                        .Where(t =>
                            t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.R3Achieved && o.value > 0) &&
                            !t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.R4Achieved && o.value > 0)
                        ).ToList()
                );

                yield return new OutcomeAnalysisEvaluation(
                    "R4 achieved",
                    OutcomeType.Positive,
                    PortfolioAnalysisKeys.R4Achieved,
                    tickerOutcomes
                        .Where(t =>
                            t.outcomes.Any(o => o.key == PortfolioAnalysisKeys.R4Achieved && o.value > 0)
                        ).ToList()
                );
        }
    }
}