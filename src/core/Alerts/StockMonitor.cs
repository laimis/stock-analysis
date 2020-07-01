using System;

namespace core.Alerts
{
    public class StockMonitor
    {
        public StockMonitor(Alert alert, AlertPricePoint pricePoint)
        {
            this.Alert = alert;
            this.PricePoint = pricePoint;
            this.Value = null;
        }

        public Alert Alert { get; }
        public AlertPricePoint PricePoint { get; }
        public double? Value { get; private set; }
        public DateTimeOffset LastTrigger { get; private set; }

        public bool UpdateValue(string ticker, double newValue, DateTimeOffset time)
        {
            if (this.Alert.State.Ticker != ticker)
            {
                return false;
            }

            if (Value == null)
            {
                Value = newValue;
                return false;
            }
                
            var prev = Value < this.PricePoint.Value;
            var curr = newValue < this.PricePoint.Value;

            if (prev != curr)
            {
                if (time.Date != LastTrigger.Date)
                {
                    LastTrigger = time;
                    return true;
                }
            }

            return false;
        }
    }
}