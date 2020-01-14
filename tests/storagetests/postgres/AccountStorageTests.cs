using System;
using System.Threading.Tasks;
using core.Account;
using storage.postgres;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Database")]
    public class AccountStorageTests
    {
        [Fact]
        public async Task StoreLogWorks()
        {
            var storage = new AccountStorage(PostgresPortfolioStorageTests._cnn);

            var entry = new LoginLogEntry("laimonas", DateTime.UtcNow);

            await storage.RecordLoginAsync(entry);

            var loadedList = await storage.GetLogins();

            Assert.NotEmpty(loadedList);
        }
    }
}