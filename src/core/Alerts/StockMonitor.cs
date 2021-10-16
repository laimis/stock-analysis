using System;

namespace core.Alerts
{
    public class StockMonitor
    {
        public StockMonitor(Alert alert, AlertPricePoint pricePoint)
        {
            Alert = alert;
            PricePoint = pricePoint;
            Value = null;
        }

        public Alert Alert { get; }
        public AlertPricePoint PricePoint { get; }
        public decimal? Value { get; private set; }
        public DateTimeOffset LastTrigger { get; private set; }
        public bool IsTriggered => LastTrigger.Date == DateTimeOffset.UtcNow.Date;

        public bool CheckTrigger(string ticker, decimal newValue, DateTimeOffset time, out StockMonitorTrigger trigger)
        {
            if (Alert.State.Ticker != ticker)
            {
                trigger = new StockMonitorTrigger();
                return false;
            }

            if (Value == null)
            {
                Value = newValue;
                trigger = new StockMonitorTrigger();
                return false;
            }
                
            var prev = Value < PricePoint.Value;
            var curr = newValue < PricePoint.Value;

            if (prev != curr)
            {
                if (time.Date != LastTrigger.Date)
                {
                    trigger = new StockMonitorTrigger(this, time, Value.Value, newValue);
                    LastTrigger = time;
                    Value = newValue;
                    return true;
                }
            }

            trigger = new StockMonitorTrigger();
            return false;
        }
    }
}