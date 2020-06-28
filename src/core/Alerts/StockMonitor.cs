namespace core.Alerts
{
    public struct StockMonitor
    {
        public StockMonitor(Alert alert)
        {
            this.Alert = alert;
            this.Value = null;
        }

        public Alert Alert { get; }
        public double? Value { get; private set; }

        public bool UpdateValue(string ticker, double newValue)
        {
            if (this.Alert.State.Ticker != ticker)
            {
                return false;
            }

            if (this.Value == null)
            {
                this.Value = newValue;
                return false;
            }

            var prev = this.Value < this.Alert.State.Threshold;
            var curr = newValue < this.Alert.State.Threshold;

            this.Value = newValue;

            return prev != curr;
        }
    }
}