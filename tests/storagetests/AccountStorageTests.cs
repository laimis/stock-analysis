using System;
using System.Linq;
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

            var users = await storage.GetUserEmailIdPairs();
            Assert.True(users.Any(u => u.Item1.Contains(email)));

            await storage.Delete(user);

            fromDb = await storage.GetUserByEmail(email);

            Assert.Null(fromDb);

            fromDb = await storage.GetUser(user.Id);

            Assert.Null(fromDb);

            users = await storage.GetUserEmailIdPairs();
            Assert.False(users.Any(u => u.Item1.Contains(email)));
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