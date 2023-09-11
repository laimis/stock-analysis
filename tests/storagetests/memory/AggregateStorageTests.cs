using System.Threading.Tasks;
using storage.memory;
using Xunit;

namespace storagetests.memory
{
    public class AggregateStorageTests
    {
        [Fact]
        public async Task HealthCheckWorks()
        {
            var storage = new MemoryAggregateStorage(new FakeOutbox());

            await storage.DoHealthCheck();
        }
    }
}