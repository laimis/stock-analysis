using System;
using System.Threading.Tasks;
using storage.postgres;
using storagetests.Fakes;
using Xunit;
using Xunit.Abstractions;

namespace storagetests.postgres
{
    [Trait("Category", "Postgres")]
    public class AggregateStorageTests
    {
        private ITestOutputHelper _output;

        public AggregateStorageTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            _output = output;
        }
        
        [Fact]
        public async Task HealthCheckWorks()
        {
            var cnn = Environment.GetEnvironmentVariable("DB_CNN");

            _output.WriteLine(cnn);
            
            var storage = new PostgresAggregateStorage(
                new FakeMediator(),
                cnn);

            await storage.DoHealthCheck();
        }
    }
}