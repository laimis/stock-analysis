using System;

namespace core.Alerts
{
    public struct StockMonitorTrigger
    {
        public StockMonitorTrigger(StockPositionMonitor m, DateTimeOffset when, decimal stopPrice, decimal value)
        {
            Monitor = m;
            When = when;
            StopPrice = stopPrice;
            Value = value;
        }

        public StockPositionMonitor Monitor { get; }
        public decimal Value { get; }
        public DateTimeOffset When { get; }
        public decimal StopPrice { get; }

        public Guid UserId => Monitor.UserId;
        public string Ticker => Monitor.Position.Ticker;
    }
}