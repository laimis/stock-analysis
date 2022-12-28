using System;
using core.Alerts;
using Xunit;

namespace coretests.Alerts
{
    public class GapUpMonitorTests
    {
        private static IStockPositionMonitor CreateMonitorUnderTest()
        {
            return new GapUpMonitor(
                "ticker", 10, System.DateTimeOffset.Now, System.Guid.NewGuid(), "This is gap up!"
            );
        }

        [Fact]
        public void Behavior()
        {
            var m = CreateMonitorUnderTest();

            var firstTrigger = m.RunCheck(7, DateTimeOffset.UtcNow);

            // no matter what, first run returns that it's triggered
            // existance of this monitor means the gap up was discovered
            Assert.True(firstTrigger);

            // subsequet fail
            var secondTrigger = m.RunCheck(7, DateTimeOffset.UtcNow);
            Assert.False(secondTrigger);
        }
    }
}