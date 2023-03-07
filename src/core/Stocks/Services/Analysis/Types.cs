using System.Collections.Generic;
using core.Shared;

namespace core.Stocks.Services.Analysis
{
    public enum OutcomeType { Positive, Negative, Neutral };

    public record AnalysisOutcome(string key, OutcomeType type, decimal value, ValueFormat valueType, string message);

    public record struct TickerOutcomes(List<AnalysisOutcome> outcomes, string ticker);

    public record struct AnalysisOutcomeEvaluation(
        string name,
        OutcomeType type,
        string sortColumn,
        List<TickerOutcomes> matchingTickers
    );

    public record struct Pattern(
        System.DateTimeOffset date,
        string name,
        string description,
        decimal value,
        ValueFormat valueFormat);
    
    public record struct TickerPatterns(List<Pattern> patterns, string ticker);

    public record struct DateScorePair(System.DateTimeOffset date, int score);

    public static class AnalysisOutcomeEvaluationScoringHelper
    {
        public static Dictionary<string, int> Generate(IEnumerable<AnalysisOutcomeEvaluation> evaluations)
        {
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
            return counts;
        }
    }
}