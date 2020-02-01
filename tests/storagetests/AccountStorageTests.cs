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

            var fromDb = await storage.GetUserByEmail(email);

            Assert.Null(fromDb);

            await storage.Save(user);

            fromDb = await storage.GetUserByEmail(email);

            Assert.NotNull(fromDb);

            Assert.NotEqual(Guid.Empty, fromDb.State.Id);
            Assert.Equal(email, fromDb.State.Email);
            Assert.Equal("firstname", fromDb.State.Firstname);
            Assert.Equal("lastname", fromDb.State.Lastname);

            await storage.Delete(user);

            fromDb = await storage.GetUserByEmail(email);

            Assert.Null(fromDb);

            fromDb = await storage.GetUser(user.Id.ToString());

            Assert.Null(fromDb);
        }

        [Fact]
        public async Task ProcessIdToUserAssociationsWork()
        {
            var storage = GetStorage();

            var r = new ProcessIdToUserAssociation(Guid.NewGuid(), DateTimeOffset.UtcNow);

            await storage.SaveUserAssociation(r);

            var fromDb = await storage.GetUserAssociation(r.Id);

            Assert.Equal(r.UserId, fromDb.UserId);
        }
    }
}