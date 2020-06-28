using System;

namespace core.Alerts
{
    public struct StockMonitorTrigger
    {
        public StockMonitorTrigger(Alert alert, TickerPrice price, DateTimeOffset when)
        {
            this.Alert = alert;
            this.Price = price;
            this.When = when;
        }

        public Alert Alert { get; }
        public TickerPrice Price { get; }
        public DateTimeOffset When { get; }
    }
}