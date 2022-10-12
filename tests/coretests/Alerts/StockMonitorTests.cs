using System;
using core.Alerts;
using core.Shared;
using core.Stocks;
using Xunit;

namespace coretests.Alerts
{
    public class StockMonitorTests
    {
        [Fact]
        public void StockMonitorBehavior()
        {
            var a = new OwnedStock(new Ticker("AMD"), System.Guid.NewGuid());
            
            a.Purchase(10, 10, DateTimeOffset.UtcNow, "notes", 9);

            var m = new StockPositionMonitor(a.State.OpenPosition, a.State.UserId);

            var triggered = m.CheckTrigger("AMD", 10, DateTimeOffset.UtcNow);

            Assert.False(triggered);

            triggered = m.CheckTrigger("AMD", 11, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            triggered = m.CheckTrigger("AMD", 8.9m, DateTimeOffset.UtcNow);
            Assert.True(triggered);
            Assert.Equal("AMD", m.Trigger.Value.ticker);
            Assert.Equal(8.9m, m.Trigger.Value.triggeredValue);

            triggered = m.CheckTrigger("BING", 52, DateTimeOffset.UtcNow);
            Assert.False(triggered);
        }
    }
}