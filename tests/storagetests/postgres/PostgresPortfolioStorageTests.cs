using System;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Stocks;
using core.Stocks.View;
using storage.postgres;
using storage.shared;
using storage.tests;
using testutils;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Postgres")]
    public class PostgresPortfolioStorageTests : PortfolioStorageTests
    {
        protected string _cnn = null;

        public PostgresPortfolioStorageTests()
        {
            _cnn = CredsHelper.GetDbCreds();
        }

        protected override IPortfolioStorage CreateStorage() =>
            new PortfolioStorage(
                new PostgresAggregateStorage(new Fakes.FakeMediator(), _cnn),
                null
            );
    }
}