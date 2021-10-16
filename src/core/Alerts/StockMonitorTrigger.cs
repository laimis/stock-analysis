using System;

namespace core.Alerts
{
    public struct StockMonitorTrigger
    {
        public StockMonitorTrigger(StockMonitor m, DateTimeOffset when, decimal oldValue, decimal newValue)
        {
            Monitor = m;
            When = when;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public StockMonitor Monitor { get; }
        public decimal OldValue { get; }
        public decimal NewValue { get; }
        public DateTimeOffset When { get; }

        public Guid UserId => Monitor.Alert.UserId;
        public string Ticker => Monitor.Alert.Ticker;
        public object Direction => OldValue < NewValue ? "UP" : "DOWN";
    }
}