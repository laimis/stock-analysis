using core;
using storage.memory;
using storage.shared;
using storage.tests;

namespace storagetests.memory
{
    public class MemoryPortfolioStorageTests : PortfolioStorageTests
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