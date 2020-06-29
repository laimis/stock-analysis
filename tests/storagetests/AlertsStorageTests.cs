using System;
using System.Linq;
using System.Threading.Tasks;
using core.Alerts;
using core.Shared;
using Xunit;

namespace storagetests
{
    public abstract class AlertsStorageTests
    {
        protected abstract IAlertsStorage GetStorage();

        [Fact]
        public async Task StoreUserWorks()
        {
            var user = Guid.NewGuid();

            var alert = new Alert(new Ticker("AMD"), user);

            var storage = GetStorage();

            var alerts = await storage.GetAlerts(user);

            Assert.Empty(alerts);

            await storage.Save(alert);

            alerts = await storage.GetAlerts(user);

            Assert.NotEmpty(alerts);

            alert = await storage.GetAlert(alerts.First().State.Ticker, user);

            Assert.NotEqual(Guid.Empty, alert.State.Id);
            Assert.Equal("AMD", alert.State.Ticker);
            
            await storage.Delete(alert);

            alerts = await storage.GetAlerts(user);

            Assert.Empty(alerts);

            alert = await storage.GetAlert("AMD", user);

            Assert.Null(alert);
        }
    }
}