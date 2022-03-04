using System;
using System.Threading.Tasks;
using storage.memory;
using storage.postgres;
using storagetests.Fakes;
using Xunit;

namespace storagetests.memory
{
    [Trait("Category", "Database")]
    public class AggregateStorageTests
    {
        [Fact]
        public async Task HealthCheckWorks()
        {
            var storage = new MemoryAggregateStorage(new FakeMediator());

            await storage.DoHealthCheck();
        }
    }
}