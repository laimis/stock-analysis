using core.Account;
using storage.postgres;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Database")]
    public class PostgresAccountStorageTests : AccountStorageTests
    {
        protected override IAccountStorage GetStorage()
        {
            return new AccountStorage(
                new Fakes.FakeMediator(),
                PostgresPortfolioStorageTests._cnn);
        }
    }
}