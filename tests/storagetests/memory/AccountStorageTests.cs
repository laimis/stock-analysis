using core.Account;
using storage.memory;
using Xunit;

namespace storagetests.memory
{
    [Trait("Category", "Database")]
    public class AccountStorageTests : AccountStorageTests
    {
        protected override IAccountStorage GetStorage()
        {
            return new AccountStorage(
                new Fakes.FakeMediator()
            );
        }
    }
}