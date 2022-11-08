using System;
using core.Alerts;
using core.Shared;
using core.Stocks;
using Xunit;

namespace coretests.Alerts
{
    public class ProfitPriceMonitorTests
    {
        [Fact]
        public void Behavior()
        {
            var m = CreateMonitorUnderTest();

            // check for a different ticker
            var triggered = m.RunCheck("BING", 7, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            // first  check, the price is not at profit
            triggered = m.RunCheck("AMD", 10, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            // second check, the price is below profit still
            triggered = m.RunCheck("AMD", 9, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            // third check, the price is in the profit zone
            triggered = m.RunCheck("AMD", 11m, DateTimeOffset.UtcNow);
            Assert.True(triggered);
            Assert.Equal("AMD", m.TriggeredAlert.Value.ticker);
            Assert.Equal(11m, m.TriggeredAlert.Value.triggeredValue);

            // fourth check, the price is in the profit zone but it was already triggered, so triggered is false while alarm gets updated
            triggered = m.RunCheck("AMD", 11.1m, DateTimeOffset.UtcNow);
            Assert.False(triggered);
            Assert.Equal("AMD", m.TriggeredAlert.Value.ticker);
            Assert.Equal(11.1m, m.TriggeredAlert.Value.triggeredValue);

            // now run check trigger but with price falling, it should be false
            triggered = m.RunCheck("AMD", 9.1m, DateTimeOffset.UtcNow);
            Assert.False(triggered);
            Assert.Null(m.TriggeredAlert);
        }

        private static ProfitPriceMonitor CreateMonitorUnderTest(int level = 0)
        {
            var a = new OwnedStock(new Ticker("AMD"), System.Guid.NewGuid());

            a.Purchase(10, 10, DateTimeOffset.UtcNow, "notes", 9);

            return ProfitPriceMonitor.CreateIfApplicable(a.State, level);
        }

        [Fact]
        public void Level2()
        {
            var m = CreateMonitorUnderTest(1);

            var triggered = m.RunCheck("AMD", 11m, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            triggered = m.RunCheck("AMD", 12m, DateTimeOffset.UtcNow);
            Assert.True(triggered);
            
            Assert.Equal("AMD", m.TriggeredAlert.Value.ticker);
            Assert.Equal(12m, m.TriggeredAlert.Value.triggeredValue);
        }
    }
}