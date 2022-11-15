using System.Collections.Generic;
using System.Linq;
using core.Stocks.Services;

namespace core.Reports.Views
{
    public struct TickerOutcomes
    {
        public TickerOutcomes(List<AnalysisOutcome> outcomes, string ticker)
        {
            Outcomes = outcomes;
            Ticker = ticker;
        }

        public string Ticker { get; }
        public List<AnalysisOutcome> Outcomes { get; }
    }

    public record struct OutcomesReportView(
        List<TickerOutcomes> outcomes,
        List<GapsView> gaps
    );

    public record struct AnalysisCategoryGrouping(
        string name,
        OutcomeType type,
        string sortColumn,
        List<TickerOutcomes> outcomes
    );

    public record struct AnalysisReportView
    {
        public IEnumerable<AnalysisCategoryGrouping> Categories { get; set; }
        public object Summary { get; set; }

        public AnalysisReportView(IEnumerable<AnalysisCategoryGrouping> categories)
        {
            Categories = categories;

            var counts = new Dictionary<string, int>();
            foreach (var category in categories)
            {
                var toAdd = category.type switch {
                    OutcomeType.Positive => 1,
                    OutcomeType.Negative => -1,
                    _ => 0
                };

                foreach(var o in category.outcomes)
                {
                    if (!counts.ContainsKey(o.Ticker))
                    {
                        counts[o.Ticker] = 0;
                    }

                    counts[o.Ticker] += toAdd;
                }
            }

            Summary = counts
                .OrderByDescending(x => x.Value)
                .Select(x => new {ticker = x.Key, count = x.Value})
                .ToArray();
        }
    }
}