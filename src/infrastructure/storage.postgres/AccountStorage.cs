using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Accounts;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.Storage;
using core.Shared;
using Dapper;
using Microsoft.FSharp.Core;
using storage.shared;

namespace storage.postgres
{
    public class AccountStorage : PostgresAggregateStorage, IAccountStorage
    {
        private const string _user_entity = "users";

        public AccountStorage(IOutbox outbox, string cnn) : base(outbox, cnn)
        {
        }

        public async Task<FSharpOption<User>> GetUser(UserId userId)
        {
            var events = await GetEventsAsync(_user_entity, userId);

            var u = new User(events);
            return u.Id == Guid.Empty ? FSharpOption<User>.None : new FSharpOption<User>(u);
        }

        public async Task<FSharpOption<User>> GetUserByEmail(string emailAddress)
        {
            emailAddress = emailAddress.ToLowerInvariant();

            string? identifier;
            using(var db = GetConnection())
            {
                var query = @"SELECT id FROM users WHERE email = :emailAddress";

                identifier = await db.QuerySingleOrDefaultAsync<string>(query, new {emailAddress});
            }

            if (identifier == null)
            {
                return FSharpOption<User>.None;
            }

            return await GetUser(UserId.NewUserId(new Guid(identifier)));
        }

        public async Task Save(User u)
        {
            using var db = GetConnection();
            using var tx = db.BeginTransaction();
            
            var query = @"INSERT INTO users (id, email) VALUES (:id, :email) ON CONFLICT DO NOTHING;";

            await db.ExecuteAsync(query, new {id = u.State.Id.ToString(), email = u.State.Email});

            await SaveEventsAsync(u, _user_entity, UserId.NewUserId(u.State.Id), outsideTransaction: tx);
        }

        public async Task Delete(User user)
        {
            using var db = GetConnection();
            using var tx = db.BeginTransaction();
            
            var query = @"DELETE FROM users WHERE id = :id";
            await db.ExecuteAsync(query, new {id = user.Id.ToString()});
            
            await DeleteAggregates(_user_entity, UserId.NewUserId(user.Id), outsideTransaction: tx);
        }

        public async Task SaveUserAssociation(ProcessIdToUserAssociation r)
        {
            using var db = GetConnection();
            
            var query = @"INSERT INTO processidtouserassociations (id, userId, timestamp) VALUES (:id, :userId, :timestamp)";

            await db.ExecuteAsync(query, new {r.Id, userId = r.UserId.Item, timestamp = r.Timestamp});
        }

        public async Task<IEnumerable<AccountBalancesSnapshot>> GetAccountBalancesSnapshots(DateTimeOffset start, DateTimeOffset end, UserId userId)
        {
            using var db = GetConnection();

            var query = @"SELECT cash,equity,longValue,shortValue,date FROM accountbalancessnapshots WHERE userId = :userId AND date BETWEEN :start AND :end ORDER BY date DESC";
            
            var result = await db.QueryAsync<AccountBalancesSnapshot>(
                query, 
                new
                {
                    userId = userId.Item,
                    start = start.ToString("yyyy-MM-dd"),
                    end = end.ToString("yyyy-MM-dd")
                });
            
            return result;
        }

        public async Task SaveAccountBalancesSnapshot(UserId userId, AccountBalancesSnapshot balances)
        {
            using var db = GetConnection();
            
            var query = @"INSERT INTO accountbalancessnapshots (cash,equity,longValue,shortValue,date,userId) VALUES (:cash,:equity,:longValue,:shortValue,:date,:userId)
ON CONFLICT (userId, date) DO UPDATE SET cash = :cash, equity = :equity, longValue = :longValue, shortValue = :shortValue";
            
            await db.ExecuteAsync(query, new
            {
                cash = balances.Cash,
                equity = balances.Equity,
                longValue = balances.LongValue,
                shortValue = balances.ShortValue,
                date = balances.Date,
                userId = userId.Item
            });
        }
        
        private string GetOrderStatusString(OrderStatus status)
        {
            //| Filled | Working | PendingActivation | Expired | Canceled | Rejected
            return status.Tag switch
            {
                OrderStatus.Tags.Filled => "Filled",
                OrderStatus.Tags.Working => "Working",
                OrderStatus.Tags.PendingActivation => "PendingActivation",
                OrderStatus.Tags.Expired => "Expired",
                OrderStatus.Tags.Canceled => "Canceled",
                OrderStatus.Tags.Rejected => "Rejected",
                OrderStatus.Tags.Accepted => "Accepted",
                _ => throw new Exception($"Unknown order status: {status}")
            };
        }
        private static OrderStatus GetOrderStatusFromString(string status) =>
            status switch
            {
                "Filled" => OrderStatus.Filled,
                "Working" => OrderStatus.Working,
                "PendingActivation" => OrderStatus.PendingActivation,
                "Expired" => OrderStatus.Expired,
                "Canceled" => OrderStatus.Canceled,
                "Rejected" => OrderStatus.Rejected,
                "Accepted" => OrderStatus.Accepted,
                _ => throw new Exception($"Unknown order status: {status}")
            };

        private static string GetOrderTypeString(OrderType orderType) =>
            orderType.Tag switch
            {
                OrderType.Tags.Limit => "Limit",
                OrderType.Tags.Market => "Market",
                OrderType.Tags.StopMarket => "Stop",
                _ => throw new Exception($"Unknown order type: {orderType}")
            };
        private static OrderType GetOrderTypeFromString(string orderType) =>
            orderType switch
            {
                "Limit" => OrderType.Limit,
                "Market" => OrderType.Market,
                "Stop" => OrderType.StopMarket,
                _ => throw new Exception($"Unknown order type: {orderType}")
            };
        
        private static string GetOrderInstructionString(OrderInstruction instruction) =>
            instruction.Tag switch
            {
                OrderInstruction.Tags.Buy => "Buy",
                OrderInstruction.Tags.Sell => "Sell",
                OrderInstruction.Tags.BuyToCover => "BuyToCover",
                OrderInstruction.Tags.SellShort => "SellShort",
                _ => throw new Exception($"Unknown order instruction: {instruction}")
            };
        private static OrderInstruction GetOrderInstructionFromString(string instruction) =>
            instruction switch
            {
                "Buy" => OrderInstruction.Buy,
                "Sell" => OrderInstruction.Sell,
                "BuyToCover" => OrderInstruction.BuyToCover,
                "SellShort" => OrderInstruction.SellShort,
                _ => throw new Exception($"Unknown order instruction: {instruction}")
            };

        private static string GetAssetTypeString(AssetType assetType) =>
            assetType.Tag switch
            {
                AssetType.Tags.Equity => "Equity",
                AssetType.Tags.Option => "Option",
                _ => throw new Exception($"Unknown asset type: {assetType}")
            };
        private static AssetType GetAssetTypeFromString(string assetType) =>
            assetType switch
            {
                "Equity" => AssetType.Equity,
                "Option" => AssetType.Option,
                _ => throw new Exception($"Unknown asset type: {assetType}")
            };

        public async Task SaveAccountBrokerageOrders(UserId userId, IEnumerable<Order> orders)
        {
            using var db = GetConnection();
            
            var query = @"INSERT INTO accountbrokerageorders (orderid,price,quantity,status,ticker,ordertype,instruction,assettype,executiontime,enteredtime,expirationtime,canbecancelled,userId,modified)
                        VALUES (:orderid,:price,:quantity,:status,:ticker,:ordertype,:instruction,:assettype,:executiontime,:enteredtime,:expirationtime,:canbecancelled,:userId,:modified)
ON CONFLICT (userId, orderid) DO UPDATE SET price = :price, quantity = :quantity, status = :status, ticker = :ticker, ordertype = :ordertype, instruction = :instruction, assettype = :assettype, executiontime = :executiontime, enteredtime = :enteredtime, expirationtime = :expirationtime, canbecancelled = :canbecancelled, modified = :modified";

            using var tx = db.BeginTransaction();
            
            foreach (var order in orders)
            {
                await db.ExecuteAsync(query, new
                {
                    orderid = order.OrderId,
                    price = order.Price,
                    quantity = order.Quantity,
                    status = GetOrderStatusString(order.Status),
                    ticker = order.Ticker.Value,
                    ordertype = GetOrderTypeString(order.Type),
                    instruction = GetOrderInstructionString(order.Instruction),
                    assettype = GetAssetTypeString(order.AssetType),
                    executiontime = FSharpOption<DateTimeOffset>.get_IsNone(order.ExecutionTime) ? null : order.ExecutionTime.Value.ToString("u"),
                    enteredtime = order.EnteredTime.ToString("u"),
                    expirationtime = FSharpOption<DateTimeOffset>.get_IsNone(order.ExpirationTime) ? null : order.ExpirationTime.Value.ToString("u"),
                    canbecancelled = order.CanBeCancelled,
                    userId = userId.Item,
                    modified = DateTimeOffset.UtcNow
                });
            }
            tx.Commit();
        }
        
        public async Task<IEnumerable<Order>> GetAccountBrokerageOrders(UserId userId)
        {
            using var db = GetConnection();
            
            var query = @"SELECT orderid,price,quantity,status,ticker,ordertype,instruction,assettype,executiontime,enteredtime,expirationtime,canbecancelled FROM accountbrokerageorders WHERE userId = :userId";

            var result = await db.QueryAsync(
                query,
                new
                {
                    userId = userId.Item
                });
            
            return result.Select(r =>
                new Order
                {
                    OrderId = r.orderid,
                    AssetType = GetAssetTypeFromString(r.assettype),
                    Instruction = GetOrderInstructionFromString(r.instruction),
                    Price = r.price,
                    Quantity = r.quantity,
                    Status = GetOrderStatusFromString(r.status),
                    Ticker = new Ticker(r.ticker),
                    Type = GetOrderTypeFromString(r.ordertype),
                    ExecutionTime = r.executiontime == null ? FSharpOption<DateTimeOffset>.None : new FSharpOption<DateTimeOffset>(DateTimeOffset.Parse(r.executiontime)),
                    EnteredTime = DateTimeOffset.Parse(r.enteredtime),
                    ExpirationTime = r.expirationtime == null ? FSharpOption<DateTimeOffset>.None : new FSharpOption<DateTimeOffset>(DateTimeOffset.Parse(r.expirationtime)),
                    CanBeCancelled = r.canbecancelled,
                });
        }

        public async Task<FSharpOption<ProcessIdToUserAssociation>> GetUserAssociation(Guid id)
        {
            using var db = GetConnection();
            
            var query = @"SELECT * FROM processidtouserassociations WHERE id = :id";

            var result = await db.QuerySingleOrDefaultAsync<ProcessIdToUserAssociation>(query, new { id });
            
            return result == null ? FSharpOption<ProcessIdToUserAssociation>.None : new FSharpOption<ProcessIdToUserAssociation>(result);
        }

        public async Task<IEnumerable<EmailIdPair>> GetUserEmailIdPairs()
        {
            using var db = GetConnection();
            
            var users = await db.QueryAsync<EmailIdPair>(
                @"SELECT email,id FROM users"
            );
            
            return users;
        }
    }
}
