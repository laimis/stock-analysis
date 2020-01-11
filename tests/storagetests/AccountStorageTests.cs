using System;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using storage.postgres;
using Xunit;

namespace storage.tests
{
    [Trait("Category", "Database")]
    public class AccountStorageTests : StorageTests
    {
        const string _userId = "testuser";

        [Fact]
        public async Task StoreLogWorks()
        {
            var storage = new AccountStorage(StorageTests._cnn);

            var entry = new LoginLogEntry("laimonas", DateTime.UtcNow);

            await storage.RecordLoginAsync(entry);

            var loadedList = await storage.GetLogins();

            Assert.NotEmpty(loadedList);
        }
    }
}