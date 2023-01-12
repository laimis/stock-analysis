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
            var stock = new OwnedStock(new Ticker("AMD"), System.Guid.NewGuid());

            stock.Purchase(10, 10, DateTimeOffset.UtcNow, "notes", 9);

            var m = ProfitPriceMonitor.CreateIfApplicable(stock.State);

            // first  check, the price is not at profit
            var triggered = m.RunCheck(10, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            // second check, the price is below profit still
            triggered = m.RunCheck(9, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            // third check, the price is in the profit zone
            triggered = m.RunCheck(11m, DateTimeOffset.UtcNow);
            Assert.True(triggered);
            Assert.Equal("AMD", m.TriggeredAlert.Value.ticker);
            Assert.Equal(11m, m.TriggeredAlert.Value.triggeredValue);

            // fourth check, the price is in the profit zone but it was already triggered, so triggered is false while alarm gets updated
            triggered = m.RunCheck(11.1m, DateTimeOffset.UtcNow);
            Assert.False(triggered);
            Assert.Equal("AMD", m.TriggeredAlert.Value.ticker);
            Assert.Equal(11.1m, m.TriggeredAlert.Value.triggeredValue);

            // now run check trigger but with price falling, it should be false
            triggered = m.RunCheck(9.1m, DateTimeOffset.UtcNow);
            Assert.False(triggered);
            Assert.Null(m.TriggeredAlert);
        }

        [Fact]
        public void Level2()
        {
            var stock = new OwnedStock(new Ticker("AMD"), System.Guid.NewGuid());

            stock.Purchase(10, 10, DateTimeOffset.UtcNow, "notes", 9);

            stock.Sell(1, 11, DateTimeOffset.UtcNow, "notes");

            var m = ProfitPriceMonitor.CreateIfApplicable(stock.State);

            var triggered = m.RunCheck(11m, DateTimeOffset.UtcNow);
            Assert.False(triggered);

            triggered = m.RunCheck(12m, DateTimeOffset.UtcNow);
            Assert.True(triggered);
            
            Assert.Equal("AMD", m.TriggeredAlert.Value.ticker);
            Assert.Equal(12m, m.TriggeredAlert.Value.triggeredValue);
        }
    }
}