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
            var storage = new AggregateStorage("localhost");

            await storage.DoHealthCheck();
        }
    }
}