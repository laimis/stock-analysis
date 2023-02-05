using System.Collections.Generic;
using System.Linq;
using core.Stocks.Services.Analysis;

namespace core.Reports.Views
{
    public record struct TickerCountPair(string ticker, int count);
    public struct OutcomesReportView
    {
        public IEnumerable<AnalysisOutcomeEvaluation> Evaluations { get; set; }
        public List<TickerOutcomes> Outcomes { get; }
        public List<GapsView> Gaps { get; }
        public List<TickerPatterns> Patterns { get; }
        public List<TickerCountPair> Summary { get; }

        public OutcomesReportView(
            IEnumerable<AnalysisOutcomeEvaluation> evaluations,
            List<TickerOutcomes> outcomes,
            List<GapsView> gaps,
            List<TickerPatterns> patterns)
        {
            Evaluations = evaluations;
            Outcomes = outcomes;
            Gaps = gaps;
            Patterns = patterns;

            var counts = AnalysisOutcomeEvaluationScoringHelper.Generate(
                evaluations
            );

            Summary = counts
                .OrderByDescending(x => x.Value)
                .Select(x => new TickerCountPair(ticker: x.Key, count: x.Value))
                .ToList();
        }
    }
}