using System;
using System.Linq;
using System.Threading.Tasks;
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

            storage.RecordLogin("laimonas");

            var loadedList = await storage.GetLogins();

            Assert.NotEmpty(loadedList);
        }
    }
}