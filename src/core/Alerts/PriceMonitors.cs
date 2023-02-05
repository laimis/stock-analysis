using System;
using core.Stocks;
using core.Stocks.Services.Analysis;
using core.Stocks.Services.Trading;

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
        AlertType alertType,
        string source,
        ValueType valueType
    )
    {
        public Guid id { get; } = Guid.NewGuid();

        internal bool MatchesTickerAndSource(TriggeredAlert a)
        {
            return this.source == a.source && this.ticker == a.ticker && a.id != this.id;
        }

        internal double AgeInHours => (DateTimeOffset.UtcNow - when).TotalHours;
    }

    public interface IStockPositionMonitor
    {
        bool RunCheck(decimal price, DateTimeOffset time);
        TriggeredAlert? TriggeredAlert { get; }
        string Ticker { get; }
        string Description { get; }
        decimal ThresholdValue { get; }
        decimal LastSeenValue { get; }
        bool IsTriggered { get; }
        AlertType AlertType { get; }
        Shared.ValueType ValueType { get; }
        Guid UserId { get; }
    }

    public enum AlertType
    {
        Negative,
        Neutral,
        Positive
    }

    public abstract class PriceMonitor : IStockPositionMonitor
    {
        protected PriceMonitor(
            decimal thresholdValue,
            decimal numberOfShares,
            string ticker,
            Guid userId,
            string description)
        {
            ThresholdValue = thresholdValue;
            NumberOfShares = numberOfShares;
            Ticker = ticker;
            UserId = userId;
            Description = description;
        }
        
        public TriggeredAlert? TriggeredAlert { get; protected set; }

        public string Ticker { get; }

        public string Description { get; }

        public decimal ThresholdValue { get; }

        public decimal LastSeenValue { get; protected set; }

        public decimal NumberOfShares { get; }

        public bool IsTriggered => TriggeredAlert.HasValue;

        public Guid UserId { get; }

        public abstract AlertType AlertType { get; }
        public Shared.ValueType ValueType => Shared.ValueType.Currency;

        public bool RunCheck(decimal price, DateTimeOffset time)
        {
            LastSeenValue = price;

            return RunCheckInternal(price, time);
        }

        protected abstract bool RunCheckInternal(decimal price, DateTimeOffset time);
    }

    public class AlwaysOnMonitor : IStockPositionMonitor
    {
        public AlwaysOnMonitor(string description, string source, string ticker, Guid userId, decimal value)
        {
            Description = description;
            Source = source;
            Ticker = ticker;
            UserId = userId;
            Value = value;
        }

        public string Ticker { get; }
        public string Description { get; }
        public string Source { get; }
        public Guid UserId { get; }
        public decimal Value { get; }
        public TriggeredAlert? TriggeredAlert { get; private set; }
        public Shared.ValueType ValueType => Shared.ValueType.Currency;
        public decimal ThresholdValue => Value;
        public decimal LastSeenValue => Value;
        public bool IsTriggered => TriggeredAlert.HasValue;
        public AlertType AlertType => AlertType.Positive;

        public bool RunCheck(decimal price, DateTimeOffset time)
        {
            if (TriggeredAlert.HasValue)
            {
                return false;
            }
            
            TriggeredAlert = new TriggeredAlert(
                price,
                price,
                time,
                Ticker,
                Description,
                0,
                UserId,
                AlertType.Positive,
                Source,
                ValueType
            );

            return true;
        }
    }

    public class GapUpMonitor : IStockPositionMonitor
    {
        public const string GapUp = "Gap up";
        public GapUpMonitor (string ticker, Gap gap, DateTimeOffset when, Guid userId)
        {
            Ticker = ticker;
            Gap = gap;
            When = when;
            UserId = userId;
            Description = GapUp;
        }

        public string Ticker { get; }
        public Gap Gap { get; }
        public DateTimeOffset When { get; }
        public Guid UserId { get; }
        public string Description { get; }
        public TriggeredAlert? TriggeredAlert { get; private set; }
        Shared.ValueType IStockPositionMonitor.ValueType => Shared.ValueType.Percentage;

        TriggeredAlert? IStockPositionMonitor.TriggeredAlert => TriggeredAlert;

        string IStockPositionMonitor.Ticker => Ticker;

        string IStockPositionMonitor.Description => Description;

        decimal IStockPositionMonitor.ThresholdValue => Gap.gapSizePct;

        decimal IStockPositionMonitor.LastSeenValue => Gap.gapSizePct;

        bool IStockPositionMonitor.IsTriggered => TriggeredAlert.HasValue;

        AlertType IStockPositionMonitor.AlertType => TriggeredAlert.HasValue ? TriggeredAlert.Value.alertType : AlertType.Neutral;

        Guid IStockPositionMonitor.UserId => UserId;

        bool IStockPositionMonitor.RunCheck(decimal price, DateTimeOffset time)
        {
            if (TriggeredAlert.HasValue)
            {
                return false;
            }

            TriggeredAlert = new TriggeredAlert(
                triggeredValue: Gap.gapSizePct,
                watchedValue: Gap.gapSizePct,
                when: time,
                ticker: Ticker,
                description: $"Gap up of {Math.Round(Gap.gapSizePct * 100, 2)}% for {Ticker}",
                numberOfShares: 0,
                userId: UserId,
                alertType: AlertType.Positive,
                source: Description,
                valueType: Shared.ValueType.Percentage
            );

            return true;
        }
    }

    public class ProfitPriceMonitor : PriceMonitor
    {
        public ProfitPriceMonitor(decimal minPrice, decimal maxPrice, int profitLevel, decimal numberOfShares, string ticker, Guid userId)
            : base(minPrice, numberOfShares, ticker, userId, $"RR{profitLevel} Profit Target")
        {
            MaxPrice = maxPrice;
        }

        public static ProfitPriceMonitor CreateIfApplicable(OwnedStockState state)
        {
            if (state.OpenPosition == null)
            {
                return null;
            }

            if (state.OpenPosition.RiskedAmount == null || state.OpenPosition.RiskedAmount == 0)
            {
                return null;
            }

            decimal minPrice = 0;
            int pricePointLevel = 1;
            for (; pricePointLevel < 10; pricePointLevel++)
            {
                minPrice = ProfitPoints.GetProfitPointWithStopPrice(state.OpenPosition, pricePointLevel).Value;
                if (minPrice > state.OpenPosition.LastSellPrice)
                {
                    break;
                }
            }

            var nextPrice = ProfitPoints.GetProfitPointWithStopPrice(state.OpenPosition, pricePointLevel + 1).Value;

            return new ProfitPriceMonitor(
                minPrice: minPrice,
                maxPrice: nextPrice,
                profitLevel: pricePointLevel,
                numberOfShares: state.OpenPosition.NumberOfShares,
                state.Ticker,
                state.UserId
            );
        }

        public override AlertType AlertType => AlertType.Positive;

        public decimal MaxPrice { get; }

        protected override bool RunCheckInternal(decimal price, DateTimeOffset time)
        {
            return IsTriggered switch {
                true => UpdateTriggeredAlert(price, time),
                false => CheckTrigger(price, time)
            };
        }

        private bool UpdateTriggeredAlert(decimal price, DateTimeOffset time)
        {
            if (price < ThresholdValue)
            {
                TriggeredAlert = null;
            }
            else if (price != TriggeredAlert.Value.triggeredValue)
            {
                SetAlert(price, time);
            }
            return false;
        }

        private bool CheckTrigger(decimal price, DateTimeOffset time)
        {
            if (price >= ThresholdValue && price < MaxPrice && !IsTriggered)
            {
                SetAlert(price, time);

                return true;
            }

            return false;
        }

        private void SetAlert(decimal price, DateTimeOffset time)
        {
            TriggeredAlert = new TriggeredAlert(
                price,
                ThresholdValue,
                time,
                Ticker,
                $"{Description} hit for {Ticker} at {price} [{ThresholdValue.ToString("0.00")} : {MaxPrice.ToString("0.00")}]",
                NumberOfShares,
                UserId,
                AlertType.Positive,
                source: Description,
                valueType: Shared.ValueType.Currency
            );
        }
    }

    public class StopPriceMonitor : PriceMonitor
    {
        public static StopPriceMonitor CreateIfApplicable(OwnedStockState state)
        {
            if (state.OpenPosition?.StopPrice == null)
            {
                return null;
            }

            return new StopPriceMonitor(
                state.OpenPosition.StopPrice.Value,
                state.OpenPosition.NumberOfShares,
                state.Ticker,
                state.UserId
            );
        }

        public StopPriceMonitor(
            decimal thresholdValue,
            decimal numberOfShares,
            string ticker,
            Guid userId)
            : base(thresholdValue, numberOfShares, ticker, userId, $"Stop loss")
        {
        }

        public override AlertType AlertType => AlertType.Negative;

        protected override bool RunCheckInternal(decimal price, DateTimeOffset time)
        {
            return IsTriggered switch {
                true => UpdateTriggeredAlert(price, time),
                false => CheckTrigger(price, time)
            };
        }

        private bool UpdateTriggeredAlert(decimal price, DateTimeOffset time)
        {
            if (price > ThresholdValue)
            {
                TriggeredAlert = null;
            }
            else if (price != TriggeredAlert.Value.triggeredValue)
            {
                SetTriggeredAlert(price, time);
            }
            return false;
        }

        private bool CheckTrigger(decimal price, DateTimeOffset time)
        {
            if (ThresholdValue > price)
            {
                SetTriggeredAlert(price, time);

                return true;
            }

            return false;
        }

        private void SetTriggeredAlert(decimal price, DateTimeOffset time)
        {
            // threshold value as string with two decimal places
            TriggeredAlert = new TriggeredAlert(
                price,
                ThresholdValue,
                time,
                Ticker,
                $"{Description} price of {ThresholdValue.ToString("0.00")} hit for {Ticker} at {price}",
                NumberOfShares,
                UserId,
                AlertType.Negative,
                source: Description,
                valueType: Shared.ValueType.Currency
            );
        }
    }
}