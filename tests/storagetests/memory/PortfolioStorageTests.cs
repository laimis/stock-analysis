using core;
using storage.memory;
using storage.shared;
using storage.tests;
using Xunit;

namespace storagetests.memory
{
    [Trait("Category", "Database")]
    public class PortfolioStorageTests : PortfolioStorageTests
    {
        protected override IPortfolioStorage CreateStorage()
        {
            return new PortfolioStorage(
                new MemoryAggregateStorage(new Fakes.FakeMediator()),
                null
            );
        }
    }
}