using System;
using core.Alerts;
using core.Stocks;
using Xunit;

namespace coretests.Alerts
{
    public class StockMonitorContainerTests
    {
        private StockAlertContainer _uat;

        public StockMonitorContainerTests()
        {
            var userId = Guid.NewGuid();
            
            _uat = new StockAlertContainer();

            var alert = StopPriceMonitor.Create(
                    price: 100,
                    stopPrice: 105,
                    ticker: "AMD",
                    when: DateTimeOffset.Now,
                    userId: userId
                );

            _uat.Register(alert);
            _uat.Register(alert);
        }

        [Fact]
        public void AlertsNotEmpty() => Assert.NotEmpty(_uat.Alerts);

        // checks that registering is idempotent
        [Fact]
        public void MonitorsCountIsCorrect() => Assert.Single(_uat.Alerts);
    }
}