using System;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
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

            var fromDbOption = await storage.GetUserByEmail(email);

            Assert.Null(fromDbOption.Value);

            await storage.Save(user);

            fromDbOption = await storage.GetUserByEmail(email);

            Assert.NotNull(fromDbOption.Value);
            
            var fromDb = fromDbOption.Value;

            Assert.NotEqual(Guid.Empty, fromDb.State.Id);
            Assert.Equal(email, fromDb.State.Email);
            Assert.Equal("firstname", fromDb.State.Firstname);
            Assert.Equal("lastname", fromDb.State.Lastname);

            var users = await storage.GetUserEmailIdPairs();
            Assert.NotEmpty(users.Where(u => u.Email == email));

            await storage.Delete(user);

            fromDbOption = await storage.GetUserByEmail(email);

            Assert.Null(fromDbOption.Value);

            fromDbOption = await storage.GetUser(user.Id);

            Assert.Null(fromDbOption.Value);

            users = await storage.GetUserEmailIdPairs();
            Assert.Empty(users.Where(u => u.Email == email));
        }

        [Fact]
        public async Task ProcessIdToUserAssociationsWork()
        {
            var storage = GetStorage();

            var r = new ProcessIdToUserAssociation(Guid.NewGuid(), DateTimeOffset.UtcNow);

            await storage.SaveUserAssociation(r);

            var fromDb = await storage.GetUserAssociation(r.Id);

            Assert.Equal(r.UserId, fromDb.Value.UserId);
        }
    }
}