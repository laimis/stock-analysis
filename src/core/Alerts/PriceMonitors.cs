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
        string source
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
        Guid UserId { get; }
        string MonitorIdentifer { get; }
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

        public abstract string MonitorIdentifer { get; }

        public bool RunCheck(decimal price, DateTimeOffset time)
        {
            LastSeenValue = price;

            return RunCheckInternal(price, time);
        }

        protected abstract bool RunCheckInternal(decimal price, DateTimeOffset time);
    }

    public class GapUpMonitor : IStockPositionMonitor
    {
        public GapUpMonitor (string ticker, Gap gap, DateTimeOffset when, Guid userId)
        {
            Ticker = ticker;
            Gap = gap;
            When = when;
            UserId = userId;
            Description = $"Gap up";
        }

        public string Ticker { get; }
        public Gap Gap { get; }
        public DateTimeOffset When { get; }
        public Guid UserId { get; }
        public string Description { get; }
        public TriggeredAlert? TriggeredAlert { get; private set; }

        TriggeredAlert? IStockPositionMonitor.TriggeredAlert => TriggeredAlert;

        string IStockPositionMonitor.Ticker => Ticker;

        string IStockPositionMonitor.Description => Description;

        decimal IStockPositionMonitor.ThresholdValue => GapSizePctRounded;

        decimal IStockPositionMonitor.LastSeenValue => GapSizePctRounded;

        private decimal GapSizePctRounded => Math.Round(Gap.gapSizePct * 100, 2);

        bool IStockPositionMonitor.IsTriggered => TriggeredAlert.HasValue;

        AlertType IStockPositionMonitor.AlertType => TriggeredAlert.HasValue ? TriggeredAlert.Value.alertType : AlertType.Neutral;

        Guid IStockPositionMonitor.UserId => UserId;

        public static string MonitorIdentifer => "GapUp";
        string IStockPositionMonitor.MonitorIdentifer => MonitorIdentifer;

        bool IStockPositionMonitor.RunCheck(decimal price, DateTimeOffset time)
        {
            if (TriggeredAlert.HasValue)
            {
                return false;
            }

            TriggeredAlert = new TriggeredAlert(
                triggeredValue: price,
                watchedValue: price,
                when: time,
                ticker: Ticker,
                description: $"Gap up of {Math.Round(Gap.gapSizePct * 100, 2)}% for {Ticker}",
                numberOfShares: 0,
                userId: UserId,
                alertType: AlertType.Positive,
                source: nameof(GapUpMonitor)
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

        public static ProfitPriceMonitor CreateIfApplicable(OwnedStockState state, int profitLevel)
        {
            if (state.OpenPosition == null)
            {
                return null;
            }

            if (state.OpenPosition.RiskedAmount == null || state.OpenPosition.RiskedAmount == 0)
            {
                return null;
            }

            var minPriceLevel = ProfitLevels.GetProfitPoint(state.OpenPosition, profitLevel);
            if (minPriceLevel == null)
            {
                return null;
            }
            
            var maxPriceLevel = ProfitLevels.GetProfitPoint(state.OpenPosition, profitLevel + 1);

            return new ProfitPriceMonitor(
                minPrice: minPriceLevel.Value,
                maxPrice: maxPriceLevel.Value,
                profitLevel: profitLevel,
                numberOfShares: state.OpenPosition.NumberOfShares,
                state.Ticker,
                state.UserId
            );
        }

        public override AlertType AlertType => AlertType.Positive;

        public decimal MaxPrice { get; }

        public override string MonitorIdentifer => $"Profit{ThresholdValue}";

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
                nameof(ProfitPriceMonitor)
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

        public override string MonitorIdentifer => $"Stop";

        private void SetTriggeredAlert(decimal price, DateTimeOffset time)
        {
            TriggeredAlert = new TriggeredAlert(
                price,
                ThresholdValue,
                time,
                Ticker,
                $"{Description} price of {ThresholdValue} hit for {Ticker} at {price}",
                NumberOfShares,
                UserId,
                AlertType.Negative,
                nameof(StopPriceMonitor)
            );
        }
    }
}