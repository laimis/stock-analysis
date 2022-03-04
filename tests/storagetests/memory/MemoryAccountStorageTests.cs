using core.Account;
using storage.memory;
using Xunit;

namespace storagetests.memory
{
    public class MemoryAccountStorageTests : AccountStorageTests
    {
        protected override IAccountStorage GetStorage()
        {
            return new AccountStorage(
                new Fakes.FakeMediator()
            );
        }
    }
}