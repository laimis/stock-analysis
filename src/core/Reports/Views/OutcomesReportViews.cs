using System.Collections.Generic;
using System.Linq;
using core.Stocks.Services;

namespace core.Reports.Views
{   
    public record struct TickerCountPair(string ticker, int count);
    public struct OutcomesReportView
    {
        public IEnumerable<AnalysisOutcomeEvaluation> Evaluations { get; set; }
        public List<TickerOutcomes> Outcomes { get; }
        public List<GapsView> Gaps { get; }
        public List<TickerCountPair> Summary { get; set; }

        public OutcomesReportView(IEnumerable<AnalysisOutcomeEvaluation> evaluations, List<TickerOutcomes> outcomes, List<GapsView> gaps)
        {
            Evaluations = evaluations;
            Outcomes = outcomes;
            Gaps = gaps;

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
                .Select(x => new TickerCountPair(ticker: x.Key, count: x.Value))
                .ToList();
        }
    }
}