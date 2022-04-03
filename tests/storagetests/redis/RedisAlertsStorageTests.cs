using core.Alerts;
using storage.redis;
using storage.shared;
using Xunit;

namespace storagetests.redis
{
    [Trait("Category", "Redis")]
    public class RedisAlertsStorageTests : AlertsStorageTests
    {
        protected override IAlertsStorage GetStorage()
        {
            return new AlertsStorage(
                new RedisAggregateStorage(
                    new Fakes.FakeMediator(),
                    "localhost"
                )
            );
        }
    }
}
