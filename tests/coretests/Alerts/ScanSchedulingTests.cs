using System;
using core.fs.Alerts;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.Storage;
using Moq;
using Xunit;

namespace coretests.Alerts
{
    public class ScanSchedulingTests
    {
        private static readonly IMarketHours _marketHours = new timezonesupport.MarketHours();
        [Theory]
        [InlineData("2023-02-28T00:00:00Z", "2023-02-28T14:45:00.0000000+00:00")] // before market open
        [InlineData("2023-02-28T21:46:00Z", "2023-02-28T22:00:00.0000000+00:00")] // after market close but not late enough to trigger next day
        [InlineData("2023-02-28T14:45:00Z", "2023-02-28T16:15:00.0000000+00:00")] // 9:45am et
        [InlineData("2023-02-28T15:45:00Z", "2023-02-28T16:15:00.0000000+00:00")] // 10:45am et
        [InlineData("2023-02-28T20:30:00Z", "2023-02-28T20:45:00.0000000+00:00")] // 3:30pm et
        [InlineData("2023-02-28T21:30:00Z", "2023-02-28T22:00:00.0000000+00:00")] // 4:30pm et
        [InlineData("2023-02-28T22:30:00Z", "2023-03-01T14:45:00.0000000+00:00")] // 5:30pm et
        [InlineData("2023-03-04T00:00:00Z", "2023-03-06T14:45:00.0000000+00:00")] // friday evening et
        public void GetNextPatternMonitor_Works(string inputUtc, string expectedUtc)
        {
            var time = DateTimeOffset.Parse(inputUtc, null, System.Globalization.DateTimeStyles.AssumeUniversal);

            var nextRun = MonitoringServices.nextPatternMonitoringRun(time, _marketHours);

            Assert.Equal(expectedUtc, nextRun.ToString("o"));
        }

        [Theory]
        [InlineData("2023-02-28T00:00:00Z", "2023-02-28T14:30:00.0000000+00:00")] // before market open
        [InlineData("2023-02-28T22:46:00Z", "2023-03-01T14:30:00.0000000+00:00")] // after market close
        [InlineData("2023-02-28T14:45:00Z", "2023-02-28T14:50:00.0000000+00:00")] // 9:45am et
        [InlineData("2023-02-28T15:45:00Z", "2023-02-28T15:50:00.0000000+00:00")] // 10:45am et
        [InlineData("2023-02-28T20:30:00Z", "2023-02-28T20:35:00.0000000+00:00")] // 3:30pm et
        [InlineData("2023-02-28T21:30:00Z", "2023-02-28T21:35:00.0000000+00:00")] // 4:30pm et
        [InlineData("2023-02-28T22:30:00Z", "2023-03-01T14:30:00.0000000+00:00")] // 4:30pm et
        [InlineData("2023-03-04T00:00:00Z", "2023-03-06T14:30:00.0000000+00:00")] // friday evening et
        public void GetNextStopLossMonitor_Works(string inputUtc, string expectedUtc)
        {
            var stopLossService = CreateStopLossMonitoringService();
            
            var time = DateTimeOffset.Parse(inputUtc, null, System.Globalization.DateTimeStyles.AssumeUniversal);

            var nextRun = stopLossService.NextRunTime(time);

            Assert.Equal(expectedUtc, nextRun.ToString("o"));
        }

        private static MonitoringServices.StopLossMonitoringService CreateStopLossMonitoringService()
        {
            var stopLossService = new MonitoringServices.StopLossMonitoringService(
                Mock.Of<IAccountStorage>(), Mock.Of<IBrokerage>(), new StockAlertContainer(),
                Mock.Of<IPortfolioStorage>(), _marketHours);
            return stopLossService;
        }

        [Theory]
        [InlineData("2023-02-28T00:00:00Z", "2023-02-28T14:30:00.0000000+00:00")] // before market open
        [InlineData("2023-02-28T22:46:00Z", "2023-03-01T14:30:00.0000000+00:00")] // after market close
        [InlineData("2023-02-28T14:45:00Z", "2023-02-28T14:50:00.0000000+00:00")] // 9:45am et
        [InlineData("2023-02-28T15:45:00Z", "2023-02-28T15:50:00.0000000+00:00")] // 10:45am et
        [InlineData("2023-02-28T20:30:00Z", "2023-02-28T20:35:00.0000000+00:00")] // 3:30pm et
        [InlineData("2023-02-28T21:30:00Z", "2023-02-28T21:35:00.0000000+00:00")] // 4:30pm et
        [InlineData("2023-03-04T00:00:00Z", "2023-03-06T14:30:00.0000000+00:00")] // friday evening et
        public void GetNextStopLossMonitor_Works2(string inputUtc, string expectedUtc)
        {
            var time = DateTimeOffset.Parse(inputUtc, null, System.Globalization.DateTimeStyles.AssumeUniversal);
            
            var service = CreateStopLossMonitoringService();

            var nextRun = service.NextRunTime(time);

            Assert.Equal(expectedUtc, nextRun.ToString("o"));
        }
    }
}
