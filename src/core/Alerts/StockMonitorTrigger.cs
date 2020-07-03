using System;

namespace core.Alerts
{
    public struct StockMonitorTrigger
    {
        public StockMonitorTrigger(StockMonitor m, DateTimeOffset when, double oldValue, double newValue)
        {
            this.Monitor = m;
            this.When = when;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        public StockMonitor Monitor { get; }
        public double OldValue { get; }
        public double NewValue { get; }
        public DateTimeOffset When { get; }

        public Guid UserId => this.Monitor.Alert.UserId;
        public string Ticker => this.Monitor.Alert.Ticker;
        public object Direction => this.OldValue < this.NewValue ? "up" : "down";
    }
}