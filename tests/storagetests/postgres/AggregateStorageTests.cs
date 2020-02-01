using System.Threading.Tasks;
using storage.postgres;
using storagetests.Fakes;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Database")]
    public class AggregateStorageTests
    {
        [Fact]
        public async Task HealthCheckWorks()
        {
            var storage = new PostgresAggregateStorage(
                new FakeMediator(),
                PostgresPortfolioStorageTests._cnn);

            await storage.DoHealthCheck();
        }
    }
}