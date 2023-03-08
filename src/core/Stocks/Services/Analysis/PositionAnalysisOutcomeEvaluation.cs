using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.SEC;

namespace core.Stocks.Services.Analysis
{
    public class PositionAnalysisOutcomeEvaluation
    {
        private const decimal PercentToStopThreshold = -0.02m;
        
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

            // see if there are any recent filings for a ticker
            yield return new AnalysisOutcomeEvaluation(
                "Recent filings",
                OutcomeType.Negative,
                PortfolioAnalysisKeys.RecentFilings,
                tickerOutcomes
                    .Where(t => filings.ContainsKey(t.ticker) && filings[t.ticker].filings.Where(f => System.DateTime.UtcNow.Subtract(f.FilingDate).TotalDays < 7).Any())
                    .ToList()
            );
        }
    }
}