using core.fs.Shared.Adapters.Storage;
using storage.memory;
using storage.shared;
using Xunit.Abstractions;

namespace storagetests.memory
{
    public class MemoryPortfolioStorageTests : PortfolioStorageTests
    {
        public MemoryPortfolioStorageTests(ITestOutputHelper output) : base(output)
        {
        }
        
        protected override IPortfolioStorage CreateStorage()
        {
            return new PortfolioStorage(
                new MemoryAggregateStorage(new FakeOutbox()),
                null
            );
        }
    }
}