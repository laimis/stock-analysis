using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
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

            string identifier;
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
            // TODO: these should be done as part of the transaction with save events async
            // or done as part of the outbox processing of the user events
            using(var db = GetConnection())
            {
                var query = @"INSERT INTO users (id, email) VALUES (:id, :email) ON CONFLICT DO NOTHING;";

                await db.ExecuteAsync(query, new {id = u.State.Id.ToString(), email = u.State.Email});
            }

            await SaveEventsAsync(u, _user_entity, UserId.NewUserId(u.State.Id));
        }

        public async Task Delete(User user)
        {
            // TODO: these should be done as part of the transaction with save events async
            // or done as part of the outbox processing of the user events
            using(var db = GetConnection())
            {
                var query = @"DELETE FROM users WHERE id = :id";
                await db.ExecuteAsync(query, new {id = user.Id.ToString()});
            }

            await DeleteAggregates(_user_entity, UserId.NewUserId(user.Id));
        }

        public async Task SaveUserAssociation(ProcessIdToUserAssociation r)
        {
            using var db = GetConnection();
            
            var query = @"INSERT INTO processidtouserassociations (id, userId, timestamp) VALUES (:id, :userId, :timestamp)";

            await db.ExecuteAsync(query, new {r.Id, userId = r.UserId.Item, timestamp = r.Timestamp});
        }

        public async Task<FSharpOption<ProcessIdToUserAssociation>> GetUserAssociation(Guid id)
        {
            using var db = GetConnection();
            
            var query = @"SELECT * FROM processidtouserassociations WHERE id = :id";

            return await db.QuerySingleOrDefaultAsync<ProcessIdToUserAssociation>(query, new { id });
        }

        public async Task<IEnumerable<EmailIdPair>> GetUserEmailIdPairs()
        {
            using var db = GetConnection();
            
            return await db.QueryAsync<EmailIdPair>(
                @"SELECT email,id FROM users"
            );
        }

        public Task<T> ViewModel<T>(Guid userId) =>
            Get<T>(typeof(T).Name + ":" + userId.ToString());

        public Task SaveViewModel<T>(T user, Guid userId) =>
            Save<T>(
                typeof(T).Name + ":" + userId.ToString(),
                user);
    }
}