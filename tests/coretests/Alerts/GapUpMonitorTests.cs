using System;
using core.Alerts;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services.Analysis;
using Xunit;

namespace coretests.Alerts
{
    public class GapUpMonitorTests
    {
        private static IStockPositionMonitor CreateMonitorUnderTest()
        {
            var bar = new PriceBar(System.DateTimeOffset.UtcNow, 0.1m, 0.12m, 0.1m, 0.12m, 100);

            var gap = new Gap(
                GapType.Up,
                0.1m,
                0.12m,
                bar,
                closedQuickly: false,
                open: true,
                relativeVolume: 1.2m,
                closingRange: 0.9m
            );

            return new GapUpMonitor(
                "ticker",
                gap,
                System.DateTimeOffset.Now,
                System.Guid.NewGuid()
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