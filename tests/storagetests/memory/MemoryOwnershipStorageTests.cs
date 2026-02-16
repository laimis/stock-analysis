using core.fs.Adapters.Storage;
using storage.memory;

namespace storagetests.memory
{
    public class MemoryOwnershipStorageTests : OwnershipStorageTests
    {
        protected override IOwnershipStorage GetStorage()
        {
            return new OwnershipStorage();
        }
    }
}
