using core.fs.Adapters.Storage;
using storage.postgres;
using testutils;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Postgres")]
    public class PostgresOwnershipStorageTests : OwnershipStorageTests
    {
        protected override IOwnershipStorage GetStorage()
        {
            return new OwnershipStorage(
                CredsHelper.GetDbCreds()
            );
        }
    }
}
