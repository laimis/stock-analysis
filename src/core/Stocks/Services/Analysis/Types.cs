using System.Collections.Generic;

namespace core.Stocks.Services.Analysis
{
    public enum OutcomeType { Positive, Negative, Neutral };

    public enum OutcomeValueType { Percentage, Currency, Number, Boolean };

    public record AnalysisOutcome(string key, OutcomeType type, decimal value, OutcomeValueType valueType, string message);

    public record struct TickerOutcomes(List<AnalysisOutcome> outcomes, string ticker);

    public record struct AnalysisOutcomeEvaluation(
        string name,
        OutcomeType type,
        string sortColumn,
        List<TickerOutcomes> matchingTickers
    );
}