using core.Account;
using storage.redis;
using Xunit;

namespace storagetests.redis
{
    [Trait("Category", "Redis")]
    public class RedisAccountStorageTests : AccountStorageTests
    {
        protected override IAccountStorage GetStorage()
        {
            return new AccountStorage(
                new Fakes.FakeMediator(),
                "localhost");
        }
    }
}
