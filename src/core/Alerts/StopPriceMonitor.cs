using System;
using core.Stocks;

namespace core.Alerts
{
    public class ProfitPriceMonitor : IStockPositionMonitor
    {
        public ProfitPriceMonitor(PositionInstance openPosition, Guid userId)
        {
            ThresholdValue = openPosition.RRLevels[0];
            NumberOfShares = openPosition.NumberOfShares;
            Ticker = openPosition.Ticker;
            UserId = userId;
        }

        public TriggeredAlert? TriggeredAlert { get; private set; }
        public bool IsTriggered => TriggeredAlert != null;
        public decimal ThresholdValue { get; }
        public decimal NumberOfShares { get; }
        public string Ticker { get; }
        public Guid UserId { get; }

        public bool RunCheck(string ticker, decimal price, DateTimeOffset time)
        {
            if (ticker != Ticker)
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
                TriggerType.Positive
            );
        }
    }

    public class StopPriceMonitor : IStockPositionMonitor
    {
        public StopPriceMonitor(PositionInstance position, Guid userId)
        {
            NumberOfShares = position.NumberOfShares;
            ThresholdValue = position.StopPrice.Value;
            Ticker = position.Ticker;
            UserId = userId;
        }

        public bool IsTriggered => TriggeredAlert != null;
        public TriggeredAlert? TriggeredAlert { get; private set; }
        
        public decimal NumberOfShares { get; }
        public decimal ThresholdValue { get; }
        public string Ticker { get; }
        public Guid UserId { get; }

        public bool RunCheck(string ticker, decimal price, DateTimeOffset time)
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
                $"Stop price of {ThresholdValue} was triggered at {price}",
                NumberOfShares,
                UserId,
                TriggerType.Negative
            );
        }
    }
}