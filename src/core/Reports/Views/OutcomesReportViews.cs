using System.Collections.Generic;
using System.Linq;
using core.Stocks.Services.Analysis;

namespace core.Reports.Views
{
    public record struct TickerCountPair(string ticker, int count);
    public record struct EvaluationCountPair(string evaluation, OutcomeType type, int count);
    public struct OutcomesReportView
    {
        public IEnumerable<AnalysisOutcomeEvaluation> Evaluations { get; set; }
        public List<TickerOutcomes> Outcomes { get; }
        public List<GapsView> Gaps { get; }
        public List<TickerPatterns> Patterns { get; }
        public List<TickerCountPair> TickerSummary { get; }
        public List<EvaluationCountPair> EvaluationSummary { get; }

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

            var tickerCounts = AnalysisOutcomeEvaluationScoringHelper.GenerateTickerCounts(
                evaluations
            );

            var evaluationCounts = AnalysisOutcomeEvaluationScoringHelper.GenerateEvaluationCounts(
                evaluations
            );

            TickerSummary = tickerCounts
                .OrderByDescending(x => x.Value)
                .Select(x => new TickerCountPair(ticker: x.Key, count: x.Value))
                .ToList();

            EvaluationSummary = evaluationCounts
                .Select(x => (x, evaluations.Single(e => e.name == x.Key)))
                .OrderBy(pair => pair.Item2.type)
                .ThenByDescending(pair => pair.Item1.Value)
                .Select(pair => {
                        var (x,category) = pair;
                        return new EvaluationCountPair(evaluation: x.Key, type: category.type, count: x.Value);
                    }
                )
                .ToList();
        }
    }
}