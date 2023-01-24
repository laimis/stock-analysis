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
        }
    }
}