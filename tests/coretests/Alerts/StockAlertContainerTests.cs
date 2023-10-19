using System;
using System.Linq;
using core.fs.Alerts;
using core.fs.Shared.Domain.Accounts;
using core.Shared;
using Xunit;

namespace coretests.Alerts
{
    public class StockAlertContainerTests
    {
        private readonly UserId _userId = UserId.NewUserId(Guid.NewGuid());
        private readonly StockAlertContainer _uat = new();

        public StockAlertContainerTests()
        {
            foreach(var _ in Enumerable.Range(0, 2))
            {
                _uat.Register(
                    TriggeredAlert.StopPriceAlert(
                        ticker: new Ticker("AMD"), price: 100, stopPrice: 105, DateTimeOffset.UtcNow, userId: _userId
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
        public void Alerts_ForDifferentUser_Empty() => Assert.Empty(_uat.GetAlerts(UserId.NewUserId(Guid.NewGuid())));
    }
}