using System;
using System.Linq;
using System.Threading.Tasks;
using storage;
using Xunit;

namespace storagetests
{
    public class AccountStorageTests
    {
        const string _userId = "testuser";

        [Fact]
        public async Task StoreLogWorks()
        {
            var storage = new AccountStorage(StorageTests._cnn);

            await storage.RecordLoginAsync("laimonas");

            var loadedList = await storage.List(0, 50);

            Assert.NotEmpty(loadedList);
        }
    }
}