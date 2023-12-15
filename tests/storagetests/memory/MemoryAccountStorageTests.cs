using core.fs.Adapters.Storage;
using storage.memory;

namespace storagetests.memory
{
    public class MemoryAccountStorageTests : AccountStorageTests
    {
        protected override IAccountStorage GetStorage()
        {
            return new AccountStorage(
                new FakeOutbox()
            );
        }
    }
}