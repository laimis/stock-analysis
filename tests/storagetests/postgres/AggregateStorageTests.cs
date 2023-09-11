using System.Threading.Tasks;
using storage.postgres;
using testutils;
using Xunit;
using Xunit.Abstractions;

namespace storagetests.postgres
{
    [Trait("Category", "Postgres")]
    public class AggregateStorageTests
    {
        private ITestOutputHelper _output;

        public AggregateStorageTests(ITestOutputHelper output)
        {
            _output = output;
        }
        
        [Fact]
        public async Task HealthCheckWorks()
        {
            var cnn = CredsHelper.GetDbCreds();
            
            var storage = new PostgresAggregateStorage(
                new FakeOutbox(),
                cnn);

            await storage.DoHealthCheck();
        }
    }
}