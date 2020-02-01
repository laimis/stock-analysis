using core;
using storage.redis;
using storage.shared;
using storage.tests;
using Xunit;

namespace storagetests.redis
{
    [Trait("Category", "Database")]
    public class RedisPortfolioStorageTests : PortfolioStorageTests
    {
        protected override IPortfolioStorage CreateStorage()
        {
            return new PortfolioStorage(
                new RedisAggregateStorage(
                    new Fakes.FakeMediator(),
                    "localhost"));
        }
    }
}