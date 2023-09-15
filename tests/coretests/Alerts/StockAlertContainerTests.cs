using System;
using System.Linq;
using core.fs.Alerts;
using Xunit;

namespace coretests.Alerts
{
    public class StockAlertContainerTests
    {
        private readonly Guid _userId = Guid.NewGuid();
        private readonly StockAlertContainer _uat = new();

        public StockAlertContainerTests()
        {
            foreach(var _ in Enumerable.Range(0, 2))
            {
                _uat.Register(
                    TriggeredAlert.StopPriceAlert(
                        ticker: "AMD", price: 100, stopPrice: 105, DateTimeOffset.Now, userId: _userId
                    )
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