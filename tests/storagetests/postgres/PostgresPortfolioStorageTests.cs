using core;
using storage.postgres;
using storage.shared;
using storage.tests;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Database")]
    public class PostgresPortfolioStorageTests : PortfolioStorageTests
    {
        internal static string _cnn = "Server=localhost;Database=stocks;User id=stocks;password=stocks";

        protected override IPortfolioStorage CreateStorage()
        {
            return new PortfolioStorage(
                new PostgresAggregateStorage(new Fakes.FakeMediator(), _cnn));
        }
    }
}