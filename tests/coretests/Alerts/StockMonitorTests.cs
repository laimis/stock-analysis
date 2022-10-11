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

            var triggered = m.CheckTrigger("AMD", 10, DateTimeOffset.UtcNow, out var trigger);

            Assert.Equal(10, m.Value);
            Assert.False(triggered);

            triggered = m.CheckTrigger("AMD", 11, DateTimeOffset.UtcNow, out trigger);
            Assert.False(triggered);

            triggered = m.CheckTrigger("AMD", 9, DateTimeOffset.UtcNow, out trigger);
            Assert.True(triggered);
            Assert.Equal("AMD", trigger.Ticker);
            Assert.Equal(9, trigger.Value);

            triggered = m.CheckTrigger("BING", 52, DateTimeOffset.UtcNow, out trigger);
            Assert.False(triggered);
        }
    }
}