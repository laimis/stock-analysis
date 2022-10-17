using System;

namespace core.Alerts
{
    public record struct TriggeredAlert(
        decimal triggeredValue,
        decimal watchedValue,
        DateTimeOffset when,
        string ticker,
        string description,
        decimal numberOfShares,
        Guid userId,
        TriggerType triggerType
    );

    public enum TriggerType
    {
        Negative,
        Neutral,
        Positive
    }

    public interface IStockPositionMonitor
    {
        bool RunCheck(string ticker, decimal price, DateTimeOffset time);
        TriggeredAlert? TriggeredAlert { get; }
        string Ticker { get; }
        string Description { get; }
        decimal ThresholdValue { get; }
        bool IsTriggered { get; }
        Guid UserId { get; }
    }
}