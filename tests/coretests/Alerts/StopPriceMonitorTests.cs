using System;
using core.Alerts;
using core.Shared;
using core.Stocks;
using Xunit;

namespace coretests.Alerts
{
    public class StopPriceMonitorTests
    {
        [Fact]
        public void Behavior()
        {
            var a = new OwnedStock(new Ticker("AMD"), System.Guid.NewGuid());
            
            a.Purchase(10, 10, DateTimeOffset.UtcNow, "notes", 9);

            var m = StopPriceMonitor.CreateIfApplicable(a.State);

            // first  check, the price is not below stop
            var triggered = m.RunCheck(10, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            // second check, the price is above stop still
            triggered = m.RunCheck(11, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            // third check, the price is below stop
            triggered = m.RunCheck(8.9m, DateTimeOffset.UtcNow);
            Assert.True(triggered);
            Assert.Equal("AMD", m.TriggeredAlert.Value.ticker);
            Assert.Equal(8.9m, m.TriggeredAlert.Value.triggeredValue);

            // fourth check, the price is below stop but it was already triggered, so triggered is false while alarm gets updated
            triggered = m.RunCheck(8.8m, DateTimeOffset.UtcNow);
            Assert.False(triggered);
            Assert.Equal("AMD", m.TriggeredAlert.Value.ticker);
            Assert.Equal(8.8m, m.TriggeredAlert.Value.triggeredValue);

            // now run check trigger but with price going up, it should be false
            triggered = m.RunCheck(9.1m, DateTimeOffset.UtcNow);
            Assert.False(triggered);
            Assert.Null(m.TriggeredAlert);
        }
    }
}