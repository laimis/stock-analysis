using core.fs.Shared.Adapters.Storage;
using storage.postgres;
using storage.shared;
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