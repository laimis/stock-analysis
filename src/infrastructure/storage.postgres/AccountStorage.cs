using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.fs.Accounts;
using core.fs.Adapters.Storage;
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

        public async Task<FSharpOption<AccountBalancesSnapshot>> GetLatestAccountBalancesSnapshot(UserId userId)
        {
            using var db = GetConnection();

            var query = @"SELECT cash,equity,longValue,shortValue,date,userId FROM accountbalancessnapshots WHERE userId = :userId ORDER BY date DESC LIMIT 1";
            
            var result = await db.QuerySingleOrDefaultAsync<AccountBalancesSnapshot>(query, new {userId = userId.Item});
            
            return result == null ? FSharpOption<AccountBalancesSnapshot>.None : new FSharpOption<AccountBalancesSnapshot>(result);
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