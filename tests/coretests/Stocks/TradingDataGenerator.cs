using System;
using System.Collections.Generic;
using core.Stocks;

namespace coretests.Stocks
{
    public class TradingDataGenerator
    {
        internal static Span<PositionInstance> GetClosedPositions()
        {
            var closedPositions = new List<PositionInstance>();

            var position = new PositionInstance("AMD");
            position.Buy(1, 100m, DateTimeOffset.Now.AddDays(-1));
            position.Sell(1, 110m, DateTimeOffset.Now);
            closedPositions.Add(position);

            position = new PositionInstance("AMD");
            position.Buy(1, 100m, DateTimeOffset.Now.AddDays(-1));
            position.Sell(1, 110m, DateTimeOffset.Now);
            closedPositions.Add(position);

            position = new PositionInstance("AMD");
            position.Buy(1, 100m, DateTimeOffset.Now.AddDays(-1));
            position.Sell(1, 90m, DateTimeOffset.Now);
            closedPositions.Add(position);

            return new Span<PositionInstance>(closedPositions.ToArray());
        }
    }
}