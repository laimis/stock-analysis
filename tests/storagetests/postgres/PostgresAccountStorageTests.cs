using core.Account;
using storage.postgres;
using testutils;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Postgres")]
    public class PostgresAccountStorageTests : AccountStorageTests
    {
        protected override IAccountStorage GetStorage()
        {
            return new AccountStorage(
                new FakeOutbox(),
                CredsHelper.GetDbCreds()
            );
        }
    }
}