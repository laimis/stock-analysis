using core.fs.Shared.Adapters.Storage;
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