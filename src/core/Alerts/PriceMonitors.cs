using System;
using core.Stocks;

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
        bool RunCheck(string ticker, decimal price, DateTimeOffset time);
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

        public bool RunCheck(string ticker, decimal price, DateTimeOffset time)
        {
            if (ticker != Ticker)
            {
                return false;
            }
            
            LastSeenValue = price;

            return RunCheckInternal(ticker, price, time);
        }

        protected abstract bool RunCheckInternal(string ticker, decimal price, DateTimeOffset time);
    }

    public class ProfitPriceMonitor : PriceMonitor
    {
        public ProfitPriceMonitor(decimal minPrice, decimal maxPrice, int profitLevel, decimal numberOfShares, string ticker, Guid userId)
            : base(minPrice, numberOfShares, ticker, userId, $"RR{profitLevel + 1} Profit Target")
        {
            MaxPrice = maxPrice;
        }

        public static ProfitPriceMonitor CreateIfApplicable(OwnedStockState state, int profitLevel)
        {
            if (state.OpenPosition == null)
            {
                return null;
            }

            if (state.OpenPosition.RRLevels.Count == 0)
            {
                return null;
            }

            return new ProfitPriceMonitor(
                minPrice: state.OpenPosition.RRLevels[profitLevel],
                maxPrice: state.OpenPosition.RRLevels[profitLevel + 1],
                profitLevel: profitLevel,
                numberOfShares: state.OpenPosition.NumberOfShares,
                state.Ticker,
                state.UserId
            );
        }

        public override AlertType AlertType => AlertType.Positive;

        public decimal MaxPrice { get; }

        public override string MonitorIdentifer => $"Profit{ThresholdValue}";

        protected override bool RunCheckInternal(string ticker, decimal price, DateTimeOffset time)
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
            : base(thresholdValue, numberOfShares, ticker, userId, $"Stop loss @ {thresholdValue.ToString("0.00")}")
        {
        }

        public override AlertType AlertType => AlertType.Negative;

        protected override bool RunCheckInternal(string ticker, decimal price, DateTimeOffset time)
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
                $"{Description} hit for {Ticker} at {price}",
                NumberOfShares,
                UserId,
                AlertType.Negative,
                nameof(StopPriceMonitor)
            );
        }
    }
}