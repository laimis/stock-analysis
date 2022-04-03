using System;
using core;
using storage.postgres;
using storage.shared;
using storage.tests;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Postgres")]
    public class PostgresPortfolioStorageTests : PortfolioStorageTests
    {
        protected string _cnn = null;

        public PostgresPortfolioStorageTests()
        {
            _cnn = Environment.GetEnvironmentVariable("DB_CNN");
        }

        protected override IPortfolioStorage CreateStorage()
        {
            
            return new PortfolioStorage(
                new PostgresAggregateStorage(new Fakes.FakeMediator(), _cnn),
                null
            );
        }
    }
}