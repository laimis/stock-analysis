using System;
using core.Account;
using core.Shared;
using core.Stocks.Services.Analysis;

// complains about lowercase property names, doing that due to json formatting
#pragma warning disable IDE1006
namespace core.Alerts
{
    public record struct AlertCheck(
        string ticker,
        string listName,
        UserState user,
        decimal? threshold = null
    );

    public record struct TriggeredAlert(
        string identifier,
        decimal triggeredValue,
        decimal watchedValue,
        DateTimeOffset when,
        string ticker,
        string description,
        string sourceList,
        Guid userId,
        AlertType alertType,
        ValueFormat valueType
    )
    {
        public Guid id { get; } = Guid.NewGuid();
        internal readonly double AgeInHours => (DateTimeOffset.UtcNow - when).TotalHours;
    }

    public enum AlertType
    {
        Negative,
        Neutral,
        Positive
    }

    public class GapUpMonitor
    {
        private const string Identifier = "Gap up";
        private static TriggeredAlert Create(
            string ticker, string sourceList,
            Gap gap, DateTimeOffset when, Guid userId)
        {
            return new TriggeredAlert(
                identifier: Identifier,
                triggeredValue: gap.gapSizePct,
                watchedValue: gap.gapSizePct,
                when: when,
                ticker: ticker,
                description: Identifier,
                sourceList: sourceList,
                userId: userId,
                alertType: AlertType.Positive,
                valueType: Shared.ValueFormat.Percentage
            );
        }

        public static void Deregister(StockAlertContainer container, string ticker, Guid userId) =>
            container.Deregister(Identifier, ticker, userId);

        public static void Register(StockAlertContainer container,
            string ticker, string sourceList, Gap gap, DateTimeOffset when, Guid userId) =>
            container.Register(Create(ticker, sourceList, gap, when, userId));
    }
    
    public class StopPriceMonitor
    {
        public const string Identifier = "Stop price";
        private static TriggeredAlert Create(
            decimal price,
            decimal stopPrice,
            string ticker,
            DateTimeOffset when,
            Guid userId)
        {
            return new TriggeredAlert(
                identifier: Identifier,
                triggeredValue: price,
                watchedValue: stopPrice,
                when: when,
                ticker: ticker,
                sourceList: Identifier,
                description: Identifier,
                userId: userId,
                alertType: AlertType.Negative,
                valueType: Shared.ValueFormat.Currency
            );
        }

        public static void Deregister(StockAlertContainer container, string ticker, Guid userId)
        {
            container.Deregister(Identifier, ticker, userId);
        }

        public static void Register(StockAlertContainer container,
            decimal price,
            decimal stopPrice,
            string ticker,
            DateTimeOffset when,
            Guid userId)
        {
            container.Register(Create(price, stopPrice, ticker, when, userId));
        }
    }

    public class PatternAlert
    {
        private static TriggeredAlert Create(
            string identifier,
            string description,
            decimal value,
            ValueFormat valueFormat,
            string ticker,
            string sourceList,
            DateTimeOffset when,
            Guid userId)
        {
            return new TriggeredAlert(
                identifier: identifier,
                triggeredValue: value,
                watchedValue: value,
                when: when,
                ticker: ticker,
                description: description,
                sourceList: sourceList,
                userId: userId,
                alertType: AlertType.Neutral,
                valueType: valueFormat
            );
        }

        public static void Register(
            StockAlertContainer container,
            string ticker,
            string sourceList,
            Pattern pattern,
            decimal value,
            ValueFormat valueFormat,
            DateTimeOffset when,
            Guid userId)
        {
            container.Register(
                Create(
                    identifier: pattern.name,
                    description: pattern.description,
                    value: value,
                    valueFormat: valueFormat,
                    ticker: ticker,
                    sourceList: sourceList,
                    when: when,
                    userId: userId
                )
            );
        }

        public static void Deregister(StockAlertContainer container, string patternName, string ticker, Guid userId)
        {
            container.Deregister(patternName, ticker, userId);
        }
    }
    #pragma warning restore IDE1006
}