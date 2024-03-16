using core.Shared;
using storage.postgres;
using testutils;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Postgres")]
    public class PostgresBlobStorageTests : BlobStorageTests
    {
        protected override IBlobStorage CreateStorage()
        {
            return new PostgresAggregateStorage(
                new FakeOutbox(),
                CredsHelper.GetDbCreds()
            );
        }
    }
}
