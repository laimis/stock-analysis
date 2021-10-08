using System;

namespace core.Alerts
{
    public struct StockMonitorTrigger
    {
        public StockMonitorTrigger(StockMonitor m, DateTimeOffset when, decimal oldValue, decimal newValue)
        {
            this.Monitor = m;
            this.When = when;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        public StockMonitor Monitor { get; }
        public decimal OldValue { get; }
        public decimal NewValue { get; }
        public DateTimeOffset When { get; }

        public Guid UserId => this.Monitor.Alert.UserId;
        public string Ticker => this.Monitor.Alert.Ticker;
        public object Direction => this.OldValue < this.NewValue ? "UP" : "DOWN";
    }
}