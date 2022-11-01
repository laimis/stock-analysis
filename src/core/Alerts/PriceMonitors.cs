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
        public ProfitPriceMonitor(decimal thresholdValue, decimal numberOfShares, string ticker, Guid userId, string description)
            : base(thresholdValue, numberOfShares, ticker, userId, description)
        {
        }

        public static ProfitPriceMonitor CreateIfApplicable(OwnedStockState state)
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
                state.OpenPosition.RRLevels[0],
                state.OpenPosition.NumberOfShares,
                state.Ticker,
                state.UserId,
                "RR1 Profit Target"
            );
        }

        public override AlertType AlertType => AlertType.Positive;

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
            if (price >= ThresholdValue && !IsTriggered)
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
                $"Profit target hit for {Ticker} at {price}",
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
                state.UserId,
                "Stop Loss"
            );
        }

        public StopPriceMonitor(
            decimal thresholdValue,
            decimal numberOfShares,
            string ticker,
            Guid userId,
            string description)
            : base(thresholdValue, numberOfShares, ticker, userId, description)
        {
        }

        public override AlertType AlertType => AlertType.Negative;

        protected override bool RunCheckInternal(string ticker, decimal price, DateTimeOffset time)
        {
            if (Ticker != ticker)
            {
                return false;
            }

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
            TriggeredAlert = new TriggeredAlert(
                price,
                ThresholdValue,
                time,
                Ticker,
                $"Stop price {ThresholdValue} for {Ticker} was triggered at {price}",
                NumberOfShares,
                UserId,
                AlertType.Negative,
                nameof(StopPriceMonitor)
            );
        }
    }
}