using System;
using core.Stocks;

namespace core.Alerts
{
    public record struct TriggeredAlert(
        decimal triggeredValue,
        decimal watchedValue,
        DateTimeOffset when,
        string ticker,
        string description,
        decimal numberOfShares,
        Guid userId,
        TriggerType triggerType
    );

    public enum TriggerType
    {
        Negative,
        Neutral,
        Positive
    }

    public interface IStockPositionMonitor
    {
        bool RunCheck(string ticker, decimal price, DateTimeOffset time);
        TriggeredAlert? TriggeredAlert { get; }
        bool IsTriggered { get; }
    }

    public class StopPriceMonitor : IStockPositionMonitor
    {
        public StopPriceMonitor(PositionInstance position, Guid userId)
        {
            Position = position;
            UserId = userId;
        }

        public bool IsTriggered => TriggeredAlert != null;
        public PositionInstance Position { get; }
        public TriggeredAlert? TriggeredAlert { get; private set; }
        public Guid UserId { get; }

        public bool RunCheck(string ticker, decimal price, DateTimeOffset time)
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
                TriggeredAlert = new TriggeredAlert(
                    price,
                    Position.StopPrice.Value,
                    time,
                    ticker,
                    $"Stop price of {Position.StopPrice.Value} was triggered at {price}",
                    Position.NumberOfShares,
                    UserId,
                    TriggerType.Negative
                );

                return true;
            }

            return false;
        }
    }
}