using System.Linq;
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
            
            a.AddPricePoint(50);

            var m = new StockMonitor(a);

            foreach(var p in m.PointValues.Values)
            {
                Assert.Null(p);
            }

            var triggered = m.UpdateValue("AMD", 50);

            Assert.True(m.PointValues.All(p => p.Value == 50));
            Assert.False(triggered);

            triggered = m.UpdateValue("AMD", 51);
            Assert.False(triggered);

            triggered = m.UpdateValue("AMD", 48);
            Assert.True(triggered);

            triggered = m.UpdateValue("BING", 52);
            Assert.False(triggered);
        }
    }
}