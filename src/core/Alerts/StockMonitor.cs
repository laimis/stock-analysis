using System;
using core.Stocks;

namespace core.Alerts
{
    public record struct StockMonitorTrigger(
        decimal triggeredValue,
        decimal watchedValue,
        DateTimeOffset when,
        string ticker,
        Guid userId
    );

    public class StockPositionMonitor
    {
        public StockPositionMonitor(PositionInstance position, Guid userId)
        {
            Position = position;
            UserId = userId;
        }

        public bool IsTriggered => Trigger != null;
        public PositionInstance Position { get; }
        public StockMonitorTrigger? Trigger { get; private set; }
        public Guid UserId { get; }

        public bool CheckTrigger(string ticker, decimal price, DateTimeOffset time)
        {
            if (Position.Ticker != ticker)
            {
                return false;
            }

            if (Position.StopPrice == null)
            {
                return false;
            }
                
            if (Position.StopPrice > price && !IsTriggered)
            {
                Trigger = new StockMonitorTrigger(price, Position.StopPrice.Value, time, ticker, UserId);
                return true;
            }

            return false;
        }
    }
}