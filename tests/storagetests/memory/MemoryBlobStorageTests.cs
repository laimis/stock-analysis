using core.Shared;
using storage.memory;

namespace storagetests.memory
{
    public class MemoryBlobStorageTests : BlobStorageTests
    {
        protected override IBlobStorage CreateStorage()
        {
            return new MemoryAggregateStorage(
                new FakeOutbox()
            );
        }
    }
}
