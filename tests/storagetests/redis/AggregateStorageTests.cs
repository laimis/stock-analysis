using System.Threading.Tasks;
using storage.redis;
using Xunit;

namespace storagetests.redis
{
    [Trait("Category", "Database")]
    public class AggregateStorageTests
    {
        [Fact]
        public async Task HealthCheckWorks()
        {
            var storage = new RedisAggregateStorage(
                new Fakes.FakeMediator(),
                "localhost");

            await storage.DoHealthCheck();
        }
    }
}