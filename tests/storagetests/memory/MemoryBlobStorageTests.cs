using core.fs.Adapters.Storage;
using storage.memory;
using storage.shared;

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