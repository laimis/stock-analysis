using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks.Services.Analysis
{
    public static class OutcomeType
    {
        public const string Positive = nameof(Positive);
        public const string Negative = nameof(Negative);
        public const string Neutral = nameof(Neutral);
    }

    public record AnalysisOutcome(
        string key,
        string type,
        decimal value,
        string valueType,
        string message
    );

    public record struct TickerOutcomes(IEnumerable<AnalysisOutcome> outcomes, string ticker);

    public record struct AnalysisOutcomeEvaluation(
        string name,
        string type,
        string sortColumn,
        List<TickerOutcomes> matchingTickers
    );

    public record struct Pattern(
        System.DateTimeOffset date,
        string name,
        string description,
        decimal value,
        string valueFormat);
    
    public record struct TickerPatterns(IEnumerable<Pattern> patterns, string ticker);

    public static class AnalysisOutcomeEvaluationScoringHelper
    {
        public static Dictionary<string, int> GenerateTickerCounts(IEnumerable<AnalysisOutcomeEvaluation> evaluations)
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

        public static Dictionary<string, int> GenerateEvaluationCounts(IEnumerable<AnalysisOutcomeEvaluation> evaluations)
        {
            var counts = new Dictionary<string, int>();
            foreach (var category in evaluations)
            {
                var key = category.name;

                if (!counts.ContainsKey(key))
                {
                    counts[key] = 0;
                }

                counts[key] += category.matchingTickers.Count;

            }
            return counts;
        }
    }
}