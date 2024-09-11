using System;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using core.fs.Adapters.Storage;
using core.fs.Accounts;
using core.fs.Adapters.Brokerage;
using core.Shared;
using Microsoft.FSharp.Core;
using storage.postgres;
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
            
            // make sure you can fetch the events if needed
            if (storage is PostgresAggregateStorage postgresStorage)
            {
                var events = await postgresStorage.GetStoredEvents("users", UserId.NewUserId(fromDb.State.Id));
                Assert.NotEmpty(events);
            }
            
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

            var date = DateTimeOffset.UtcNow;
            
            var balances = new AccountBalancesSnapshot(100, 200, 300, 400, date.ToString("yyyy-MM-dd"));

            await storage.SaveAccountBalancesSnapshot(userId, balances);
            
            var fromDb = await storage.GetAccountBalancesSnapshots(date, date, userId);
            
            Assert.Single(fromDb);

            Assert.Equal(balances.Cash, fromDb.First().Cash);
            Assert.Equal(balances.LongValue, fromDb.First().LongValue);
            Assert.Equal(balances.ShortValue, fromDb.First().ShortValue);
            Assert.Equal(balances.Date, fromDb.First().Date);
            
            // saving the same snapshot should not blow up but just do update
            await storage.SaveAccountBalancesSnapshot(userId, balances);
        }

        [Fact]
        public async Task AccountOrdersWork()
        {
            var storage = GetStorage();
            
            var userId = UserId.NewUserId(Guid.NewGuid());

            var order = new Order
            {
                Instruction = OrderInstruction.Sell,
                Price = 10m,
                Quantity = 1,
                Status = OrderStatus.Working,
                Ticker = new Ticker("AAPL"),
                Type = OrderType.Limit,
                AssetType = AssetType.Equity,
                EnteredTime = DateTimeOffset.UtcNow,
                ExpirationTime = DateTimeOffset.UtcNow,
                ExecutionTime = FSharpOption<DateTimeOffset>.None,
                OrderId = "123",
                CanBeCancelled = true
            };
            
            await storage.SaveAccountBrokerageOrders(userId, new [] { order });
            
            var fromDb = await storage.GetAccountBrokerageOrders(userId);
            
            Assert.Single(fromDb);
            
            var fromDbOrder = fromDb.First();
            
            Assert.Equal(order.Instruction, fromDbOrder.Instruction);
            Assert.Equal(order.Price, fromDbOrder.Price);
            Assert.Equal(order.Quantity, fromDbOrder.Quantity);
            Assert.Equal(order.Status, fromDbOrder.Status);
            Assert.Equal(order.Ticker.Value, fromDbOrder.Ticker.Value);
            Assert.Equal(order.Type, fromDbOrder.Type);
            Assert.Equal(order.AssetType, fromDbOrder.AssetType);
            Assert.Equal(order.EnteredTime, fromDbOrder.EnteredTime, TimeSpan.FromSeconds(1));
            Assert.Equal(order.ExpirationTime.Value, fromDbOrder.ExpirationTime.Value, TimeSpan.FromSeconds(1));
            Assert.Equal(order.ExecutionTime, fromDbOrder.ExecutionTime);
            Assert.Equal(order.OrderId, fromDbOrder.OrderId);
            Assert.Equal(order.CanBeCancelled, fromDbOrder.CanBeCancelled);
        }
        
        [Fact]
        public async Task AccountTransactionsWork()
        {
            var storage = GetStorage();
            
            var userId = UserId.NewUserId(Guid.NewGuid());

            var transaction = new AccountTransaction
            {
                NetAmount = 10m,
                Type = AccountTransactionType.Dividend,
                TradeDate = DateTimeOffset.UtcNow,
                SettlementDate = DateTimeOffset.UtcNow,
                Description = "desc",
                Ticker = new Ticker("AAPL"),
                TransactionId = "123",
            };
            
            await storage.SaveAccountBrokerageTransactions(userId, new [] { transaction });
            
            var fromDb = await storage.GetAccountBrokerageTransactions(userId);
            
            Assert.Single(fromDb);
            
            var fromDbTransaction = fromDb.First();
            
            Assert.Equal(transaction.NetAmount, fromDbTransaction.NetAmount);
            Assert.Equal(transaction.Type, fromDbTransaction.Type);
            Assert.Equal(transaction.SettlementDate, fromDbTransaction.SettlementDate, TimeSpan.FromSeconds(1));
            Assert.Equal(transaction.TradeDate, fromDbTransaction.TradeDate, TimeSpan.FromSeconds(1));
            Assert.Equal(transaction.Description, fromDbTransaction.Description);
            Assert.Equal(transaction.Ticker.Value, fromDbTransaction.Ticker.Value);
            Assert.Equal(transaction.Type, fromDbTransaction.Type);
        }
    }
}
