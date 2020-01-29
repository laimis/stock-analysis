using System;
using System.Threading.Tasks;
using core.Account;
using Xunit;

namespace storagetests
{
    public abstract class AccountStorageTests
    {
        protected abstract IAccountStorage GetStorage();

        [Fact]
        public async Task StoreLogWorks()
        {
            var storage = GetStorage();

            var entry = new LoginLogEntry("laimonas", DateTime.UtcNow);

            await storage.RecordLoginAsync(entry);

            var loadedList = await storage.GetLogins();

            Assert.NotEmpty(loadedList);
        }

        [Fact]
        public async Task StoreUserWorks()
        {
            var email = Guid.NewGuid().ToString();

            var user = new User(email);

            var storage = GetStorage();

            var fromDb = await storage.GetUser(email);

            Assert.Null(fromDb);

            await storage.Save(user);

            fromDb = await storage.GetUser(email);

            Assert.NotNull(fromDb);

            Assert.NotEqual(Guid.Empty, fromDb.State.Id);
        }
    }
}