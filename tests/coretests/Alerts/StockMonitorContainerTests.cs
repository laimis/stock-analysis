using System;
using System.Linq;
using core.Alerts;
using Xunit;

namespace coretests.Alerts
{
    public class StockMonitorContainerTests
    {
        private Guid _userId;
        private StockAlertContainer _uat;

        public StockMonitorContainerTests()
        {
            _userId = Guid.NewGuid();
            
            _uat = new StockAlertContainer();

            foreach(var _ in Enumerable.Range(0, 2))
            {
                StopPriceMonitor.Register(
                    container: _uat,
                    price: 100,
                    stopPrice: 105,
                    ticker: "AMD",
                    when: DateTimeOffset.Now,
                    userId: _userId
                );
            }
        }

        [Fact]
        public void Alerts_ForUser_NotEmpty() => Assert.NotEmpty(_uat.GetAlerts(_userId));

        // checks that registering is idempotent
        [Fact]
        public void Alerts_ForUser_OnlyOne() => Assert.Single(_uat.GetAlerts(_userId));

        [Fact]
        public void Alerts_ForDifferentUser_Empty() => Assert.Empty(_uat.GetAlerts(Guid.NewGuid()));
    }
}