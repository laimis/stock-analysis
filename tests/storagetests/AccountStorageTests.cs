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
        public async Task StoreUserWorks()
        {
            var email = Guid.NewGuid().ToString();

            var user = new User(email, "firstname", "lastname");

            var storage = GetStorage();

            var fromDb = await storage.GetUser(email);

            Assert.Null(fromDb);

            await storage.Save(user);

            fromDb = await storage.GetUser(email);

            Assert.NotNull(fromDb);

            Assert.NotEqual(Guid.Empty, fromDb.State.Id);
            Assert.Equal(email, fromDb.State.Email);
            Assert.Equal("firstname", fromDb.State.Firstname);
            Assert.Equal("lastname", fromDb.State.Lastname);
        }
    }
}