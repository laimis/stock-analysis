using System;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using storage;
using Xunit;

namespace storagetests
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

            storage.RecordLogin(entry);

            var loadedList = await storage.GetLogins();

            Assert.NotEmpty(loadedList);
        }
    }
}