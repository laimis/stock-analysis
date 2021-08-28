using System;
using System.Threading.Tasks;
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
            var redisStorage = new RedisAggregateStorage(
                new Fakes.FakeMediator(),
                "localhost"
            );

            return new PortfolioStorage(redisStorage, redisStorage);
        }

        [Fact]
        public async Task Test()
        {
            var storage = CreateStorage();

            var stock = await storage.GetStock("FSLY", Guid.Parse("2d57a1ae-21f5-47de-b4f3-517a4a68fd27"));

            Assert.Single(stock.State.PositionInstances);
            Assert.Equal(408, stock.State.PositionInstances[0].DaysHeld);
        }
    }
}