using System;
using core.Stocks;

namespace core.Alerts
{
    public class StockPositionMonitor
    {
        public StockPositionMonitor(PositionInstance position, Guid userId)
        {
            Position = position;
            UserId = userId;
        }

        public DateTimeOffset? LastTrigger { get; private set; }
        public bool IsTriggered => LastTrigger != null;
        public decimal Value { get; private set; }
        public PositionInstance Position { get; }
        public Guid UserId { get; }

        public bool CheckTrigger(string ticker, decimal newValue, DateTimeOffset time, out StockMonitorTrigger trigger)
        {
            if (Position.Ticker != ticker)
            {
                trigger = new StockMonitorTrigger();
                return false;
            }

            if (Position.StopPrice == null)
            {
                trigger = new StockMonitorTrigger();
                return false;
            }
                
            if (Position.StopPrice > newValue && LastTrigger == null)
            {
                trigger = new StockMonitorTrigger(this, time, Value, newValue);
                LastTrigger = time;
                Value = newValue;
                return true;
            }

            trigger = new StockMonitorTrigger();
            return false;
        }
    }
}