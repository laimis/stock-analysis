using core;
using storage.postgres;
using storage.shared;
using storage.tests;
using testutils;
using Xunit;
using Xunit.Abstractions;

namespace storagetests.postgres
{
    [Trait("Category", "Postgres")]
    public class PostgresPortfolioStorageTests : PortfolioStorageTests
    {
        protected string _cnn = null;

        public PostgresPortfolioStorageTests(ITestOutputHelper output) : base(output)
        {
            _cnn = CredsHelper.GetDbCreds();
        }

        protected override IPortfolioStorage CreateStorage() =>
            new PortfolioStorage(
                new PostgresAggregateStorage(new FakeOutbox(), _cnn),
                null
            );
    }
}