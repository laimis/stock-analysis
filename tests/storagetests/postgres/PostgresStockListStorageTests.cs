using core.fs.Adapters.Storage;
using storage.postgres;
using testutils;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Postgres")]
    public class PostgresStockListStorageTests : StockListStorageTests
    {
        protected override IStockListStorage CreateStorage() =>
            new AccountStorage(
                new FakeOutbox(),
                CredsHelper.GetDbCreds()
            );
    }
}
