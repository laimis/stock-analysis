using System.Collections.Generic;
using System.Linq;
using core.Stocks.Services;

namespace core.Reports.Views
{
    
    public record struct OutcomesReportView(
        AnalysisReportView analysis,
        List<TickerOutcomes> outcomes,
        List<GapsView> gaps
    );

    public record struct AnalysisReportView
    {
        public IEnumerable<OutcomeAnalysisEvaluation> Evaluations { get; set; }
        public object Summary { get; set; }

        public AnalysisReportView(IEnumerable<OutcomeAnalysisEvaluation> evaluations)
        {
            Evaluations = evaluations;

            var counts = new Dictionary<string, int>();
            foreach (var category in evaluations)
            {
                var toAdd = category.type switch {
                    OutcomeType.Positive => 1,
                    OutcomeType.Negative => -1,
                    _ => 0
                };

                foreach(var o in category.matchingTickers)
                {
                    if (!counts.ContainsKey(o.ticker))
                    {
                        counts[o.ticker] = 0;
                    }

                    counts[o.ticker] += toAdd;
                }
            }

            Summary = counts
                .OrderByDescending(x => x.Value)
                .Select(x => new {ticker = x.Key, count = x.Value})
                .ToArray();
        }
    }
}