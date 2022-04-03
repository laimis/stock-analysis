using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using Dapper;
using MediatR;

namespace storage.postgres
{
    public class AccountStorage : PostgresAggregateStorage, IAccountStorage
    {
        private const string _user_entity = "users";

        public AccountStorage(IMediator mediator, string cnn) : base(mediator, cnn)
        {
        }

        public async Task<User> GetUser(Guid userId)
        {
            var events = await GetEventsAsync(_user_entity, userId);

            var u = new User(events);
            if (u.Id == Guid.Empty)
            {
                return null;
            }
            return u;
        }

        public async Task<User> GetUserByEmail(string emailAddress)
        {
            emailAddress = emailAddress.ToLowerInvariant();

            using var db = GetConnection();
            db.Open();
            var query = @"SELECT id FROM users WHERE email = :emailAddress";

            var identifier = await db.QuerySingleOrDefaultAsync<string>(query, new {emailAddress});
            if (identifier == null)
            {
                return null;
            }

            return await GetUser(new Guid(identifier));
        }

        public async Task Save(User u)
        {
            using var db = GetConnection();
            db.Open();
            var query = @"INSERT INTO users (id, email) VALUES (:id, :email) ON CONFLICT DO NOTHING;";

            await db.ExecuteAsync(query, new {id = u.State.Id.ToString(), email = u.State.Email});

            await SaveEventsAsync(u, _user_entity, u.State.Id);
        }

        public async Task Delete(User user)
        {
            using var db = GetConnection();
            db.Open();
            var query = @"DELETE FROM users WHERE id = :id";

            await db.ExecuteAsync(query, new {id = user.Id.ToString()});

            await DeleteAggregates(_user_entity, user.Id);
        }

        public async Task SaveUserAssociation(ProcessIdToUserAssociation r)
        {
            using var db = GetConnection();
            db.Open();
            var query = @"INSERT INTO processidtouserassociations (id, userId, timestamp) VALUES (:id, :userId, :timestamp)";

            await db.ExecuteAsync(query, new {r.Id, userId = r.UserId, timestamp = r.Timestamp});
        }

        public async Task<ProcessIdToUserAssociation> GetUserAssociation(Guid id)
        {
            using var db = GetConnection();
            db.Open();
            var query = @"SELECT * FROM processidtouserassociations WHERE id = :id";

            return await db.QuerySingleOrDefaultAsync<ProcessIdToUserAssociation>(query, new { id });
        }

        public async Task<IEnumerable<(string email, string id)>> GetUserEmailIdPairs()
        {
            using var db = GetConnection();
            
            db.Open();
            var query = @"SELECT email,id FROM users";

            return await db.QueryAsync<(string, string)>(query);
        }

        public Task<T> ViewModel<T>(Guid userId)
        {
            return Task.FromResult<T>(default(T));
        }

        public Task SaveViewModel<T>(T user, Guid userId)
        {
            return Task.CompletedTask;
        }
    }
}