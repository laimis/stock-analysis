using System.Threading.Tasks;
using storage.postgres;
using Xunit;

namespace storagetests.postgres
{
    public class AggregateStorageTests
    {
        [Fact]
        public async Task HealthCheckWorks()
        {
            var storage = new AggregateStorage(PostgresPortfolioStorageTests._cnn);

            await storage.DoHealthCheck();
        }
    }
}