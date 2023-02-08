using System;
using core.Stocks.Services.Analysis;

namespace core.Alerts
{
    public record struct TriggeredAlert(
        decimal triggeredValue,
        decimal watchedValue,
        DateTimeOffset when,
        string ticker,
        string description,
        Guid userId,
        AlertType alertType,
        ValueType valueType
    )
    {
        public Guid id { get; } = Guid.NewGuid();
        internal double AgeInHours => (DateTimeOffset.UtcNow - when).TotalHours;
    }

    public enum AlertType
    {
        Negative,
        Neutral,
        Positive
    }

    public class GapUpMonitor
    {
        public const string GapUp = "Gap up";
        
        public static TriggeredAlert Create(string ticker, Gap gap, DateTimeOffset when, Guid userId)
        {
            return new TriggeredAlert(
                triggeredValue: gap.gapSizePct,
                watchedValue: gap.gapSizePct,
                when: when,
                ticker: ticker,
                description: GapUp,
                userId: userId,
                alertType: AlertType.Positive,
                valueType: Shared.ValueType.Percentage
            );
        }
    }

    public class ProfitPriceMonitor
    {
        public const string Description = "Profit target";

        public static TriggeredAlert Create(
            decimal price,
            decimal watchedValue,
            string ticker,
            DateTimeOffset when,
            Guid userId)
        {
            return new TriggeredAlert(
                triggeredValue: price,
                watchedValue: watchedValue,
                when: when,
                ticker: ticker,
                description: Description,
                userId: userId,
                alertType: AlertType.Positive,
                valueType: Shared.ValueType.Currency
            );
        }
    }

    public class StopPriceMonitor
    {
        public const string Description = "Stop loss";
        
        public static TriggeredAlert Create(
            decimal price,
            decimal stopPrice,
            string ticker,
            DateTimeOffset when,
            Guid userId)
        {
            return new TriggeredAlert(
                triggeredValue: price,
                watchedValue: stopPrice,
                when: when,
                ticker: ticker,
                description: Description,
                userId: userId,
                alertType: AlertType.Negative,
                valueType: Shared.ValueType.Currency
            );
        }
    }

    public class UpsideReversalAlert
    {
        public const string Description = "Upside reversal";
        
        public static TriggeredAlert Create(
            decimal price,
            string ticker,
            DateTimeOffset when,
            Guid userId)
        {
            return new TriggeredAlert(
                triggeredValue: price,
                watchedValue: price,
                when: when,
                ticker: ticker,
                description: Description,
                userId: userId,
                alertType: AlertType.Positive,
                valueType: Shared.ValueType.Currency
            );
        }
    }
}