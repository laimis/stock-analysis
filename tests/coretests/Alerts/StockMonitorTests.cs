using System;
using System.Collections.Concurrent;
using core.Alerts;
using core.Shared;
using Xunit;

namespace coretests.Alerts
{
    public class StockMonitorTests
    {
        [Fact]
        public void StockMonitorBehavior()
        {
            var a = new Alert(new Ticker("AMD"), System.Guid.NewGuid());
            
            a.AddPricePoint("initial", 50);

            var m = new StockMonitor(a, a.PricePoints[0]);

            Assert.Null(m.Value);

            var triggered = m.CheckTrigger("AMD", 50, DateTimeOffset.UtcNow, out var trigger);

            Assert.Equal(50, m.Value);
            Assert.False(triggered);

            triggered = m.CheckTrigger("AMD", 51, DateTimeOffset.UtcNow, out trigger);
            Assert.False(triggered);

            triggered = m.CheckTrigger("AMD", 48, DateTimeOffset.UtcNow, out trigger);
            Assert.True(triggered);
            Assert.Equal("AMD", trigger.Ticker);
            Assert.Equal(48, trigger.NewValue);
            Assert.Equal("down", trigger.Direction);

            triggered = m.CheckTrigger("BING", 52, DateTimeOffset.UtcNow, out trigger);
            Assert.False(triggered);
        }
    }
}