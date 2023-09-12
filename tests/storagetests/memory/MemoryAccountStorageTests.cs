using core.Account;
using core.Shared.Adapters.Storage;
using storage.memory;
using Xunit;

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