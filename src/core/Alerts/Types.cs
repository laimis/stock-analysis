using System;
using core.Account;
using core.Shared;
using core.Stocks.Services.Analysis;

namespace core.Alerts
{
    public record struct AlertCheck(
        string ticker,
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
        Guid userId,
        AlertType alertType,
        ValueFormat valueType
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
        private const string Identifier = "Gap up";
        private static TriggeredAlert Create(
            string ticker,
            Gap gap, DateTimeOffset when, Guid userId)
        {
            return new TriggeredAlert(
                identifier: Identifier,
                triggeredValue: gap.gapSizePct,
                watchedValue: gap.gapSizePct,
                when: when,
                ticker: ticker,
                description: Identifier,
                userId: userId,
                alertType: AlertType.Positive,
                valueType: Shared.ValueFormat.Percentage
            );
        }

        public static void Deregister(StockAlertContainer container, string ticker, Guid userId) =>
            container.Deregister(Identifier, ticker, userId);

        public static void Register(StockAlertContainer container,
            string ticker, Gap gap, DateTimeOffset when, Guid userId) =>
            container.Register(Create(ticker, gap, when, userId));
    }

    public class StopPriceMonitor
    {
        private const string Identifier = "Stop price";
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
            decimal price,
            string ticker,
            DateTimeOffset when,
            Guid userId)
        {
            return new TriggeredAlert(
                identifier: identifier,
                triggeredValue: price,
                watchedValue: price,
                when: when,
                ticker: ticker,
                description: description,
                userId: userId,
                alertType: AlertType.Positive,
                valueType: Shared.ValueFormat.Currency
            );
        }

        public static void Register(
            StockAlertContainer container,
            string ticker,
            Pattern pattern,
            decimal price,
            DateTimeOffset when,
            Guid userId)
        {
            container.Register(
                Create(
                    identifier: pattern.name,
                    description: pattern.description,
                    price: price,
                    ticker: ticker,
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
}