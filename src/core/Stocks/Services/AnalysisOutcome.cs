namespace core.Stocks.Services
{
    public enum OutcomeType { Positive, Negative, Neutral };

    public record AnalysisOutcome(string key, OutcomeType type, decimal value, string message);
}