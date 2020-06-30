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

        public bool UpdateValue(string ticker, double newValue)
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

            return prev != curr;
        }
    }
}