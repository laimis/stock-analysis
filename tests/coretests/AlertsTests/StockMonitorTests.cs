using core.Alerts;
using core.Shared;
using Xunit;

namespace coretests.AlertsTests
{
    public class StockMonitorTests
    {
        [Fact]
        public void StockMonitorBehavior()
        {
            var a = new Alert(new Ticker("AMD"), System.Guid.NewGuid(), 50, true);

            var m = new StockMonitor(a);

            Assert.Null(m.Value);

            var triggered = m.UpdateValue("AMD", 50);

            Assert.Equal(50, m.Value);
            Assert.True(triggered);

            triggered = m.UpdateValue("AMD", 51);
            Assert.False(triggered);

            triggered = m.UpdateValue("AMD", 48);
            Assert.True(triggered);

            triggered = m.UpdateValue("BING", 52);
            Assert.False(triggered);
        }
    }
}