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

            var m = new StopPriceMonitor(a.State.OpenPosition, a.State.UserId);

            var triggered = m.RunCheck("AMD", 10, DateTimeOffset.UtcNow);

            Assert.False(triggered);

            triggered = m.RunCheck("AMD", 11, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            triggered = m.RunCheck("AMD", 8.9m, DateTimeOffset.UtcNow);
            Assert.True(triggered);
            Assert.Equal("AMD", m.TriggeredAlert.Value.ticker);
            Assert.Equal(8.9m, m.TriggeredAlert.Value.triggeredValue);

            triggered = m.RunCheck("BING", 52, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            // now run check trigger but with price going up, it should be false
            triggered = m.RunCheck("AMD", 9.1m, DateTimeOffset.UtcNow);
            Assert.False(triggered);
            Assert.Null(m.TriggeredAlert);
        }
    }
}