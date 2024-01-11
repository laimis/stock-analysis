using System;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using core.fs.Adapters.Storage;
using core.fs.Accounts;
using Microsoft.FSharp.Core;
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

            var user = User.Create(email, "firstname", "lastname");

            var storage = GetStorage();

            var fromDbOption = await storage.GetUserByEmail(email);

            Assert.True(FSharpOption<User>.get_IsNone(fromDbOption));

            await storage.Save(user);

            fromDbOption = await storage.GetUserByEmail(email);

            Assert.NotNull(fromDbOption.Value);
            
            var fromDb = fromDbOption.Value;

            Assert.NotEqual(Guid.Empty, fromDb.State.Id);
            Assert.Equal(email, fromDb.State.Email);
            Assert.Equal("firstname", fromDb.State.Firstname);
            Assert.Equal("lastname", fromDb.State.Lastname);

            // change settings, save, reload
            fromDb.SetSetting("maxLoss", "60");
            
            await storage.Save(fromDb);
            
            fromDbOption = await storage.GetUserByEmail(email);
            
            Assert.NotNull(fromDbOption.Value);
            Assert.Equal(60, fromDbOption.Value.State.MaxLoss);

            var users = await storage.GetUserEmailIdPairs();
            Assert.NotEmpty(users.Where(u => u.Email == email));
            
            await storage.Delete(user);

            fromDbOption = await storage.GetUserByEmail(email);

            Assert.True(FSharpOption<User>.get_IsNone(fromDbOption));

            fromDbOption = await storage.GetUser(UserId.NewUserId(user.Id));

            Assert.True(FSharpOption<User>.get_IsNone(fromDbOption));

            users = await storage.GetUserEmailIdPairs();
            Assert.Empty(users.Where(u => u.Email == email));
        }

        [Fact]
        public async Task ProcessIdToUserAssociationsWork()
        {
            var storage = GetStorage();

            var r = new ProcessIdToUserAssociation(UserId.NewUserId(Guid.NewGuid()), DateTimeOffset.UtcNow);

            await storage.SaveUserAssociation(r);

            var fromDb = await storage.GetUserAssociation(r.Id);

            Assert.Equal(r.UserId, fromDb.Value.UserId);
        }
        
        [Fact]
        public async Task AccountBalancesSnapshotWorks()
        {
            var storage = GetStorage();

            var userId = UserId.NewUserId(Guid.NewGuid());

            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            
            var balances = new AccountBalancesSnapshot(100, 200, 300, 400, date, userId.Item);

            await storage.SaveAccountBalancesSnapshot(userId, balances);
            
            var fromDb = await storage.GetLatestAccountBalancesSnapshot(userId);

            Assert.Equal(balances.UserId, fromDb.Value.UserId);
            Assert.Equal(balances.Cash, fromDb.Value.Cash);
            Assert.Equal(balances.LongValue, fromDb.Value.LongValue);
            Assert.Equal(balances.ShortValue, fromDb.Value.ShortValue);
            Assert.Equal(balances.Date, fromDb.Value.Date);
            
            // saving the same snapshot should not blow up but just do update
            await storage.SaveAccountBalancesSnapshot(userId, balances);
        }
    }
}