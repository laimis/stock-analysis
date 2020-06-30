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
            
            a.AddPricePoint(50);

            var m = new StockMonitor(a, a.PricePoints[0]);

            Assert.Null(m.Value);

            var triggered = m.UpdateValue("AMD", 50);

            Assert.Equal(50, m.Value);
            Assert.False(triggered);

            triggered = m.UpdateValue("AMD", 51);
            Assert.False(triggered);

            triggered = m.UpdateValue("AMD", 48);
            Assert.True(triggered);

            triggered = m.UpdateValue("BING", 52);
            Assert.False(triggered);
        }

        [Fact]
        public void QueueTests()
        {
            var msg = new ConcurrentQueue<string>();

            msg.Enqueue("message1");
            msg.Enqueue("message2");
            msg.Enqueue("message3");

            msg.TryDequeue(out var r);

            Assert.Equal("message1", r);
            
        }
    }
}