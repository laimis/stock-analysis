using System.Collections.Generic;

namespace core.Stocks.Services
{
    public enum OutcomeType { Positive, Negative, Neutral };

    public record AnalysisOutcome(string key, OutcomeType type, decimal value, string message);

    public record struct TickerOutcomes(List<AnalysisOutcome> outcomes, string ticker);

    public record struct AnalysisOutcomeEvaluation(
        string name,
        OutcomeType type,
        string sortColumn,
        List<TickerOutcomes> matchingTickers
    );
}