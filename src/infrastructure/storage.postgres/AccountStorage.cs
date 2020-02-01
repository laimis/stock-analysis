using System;
using System.Threading.Tasks;
using core.Account;
using Dapper;

namespace storage.postgres
{
    public class AccountStorage : PostgresAggregateStorage, IAccountStorage
    {
        private const string _user_entity = "users";

        public AccountStorage(string cnn) : base(cnn)
        {
        }

        public async Task<User> GetUser(string userId)
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
            using var db = GetConnection();
            db.Open();
            var query = @"SELECT id FROM users WHERE email = :emailAddress";

            var identifier = await db.QuerySingleOrDefaultAsync<string>(query, new {emailAddress});
            if (identifier == null)
            {
                return null;
            }

            return await GetUser(identifier);
        }

        public async Task Save(User u)
        {
            using var db = GetConnection();
            db.Open();
            var query = @"INSERT INTO users (id, email) VALUES (:id, :email) ON CONFLICT DO NOTHING;";

            await db.ExecuteAsync(query, new {id = u.State.Id.ToString(), email = u.State.Email});

            await SaveEventsAsync(u, _user_entity, u.State.Id.ToString());
        }

        public async Task Delete(string userId, string email)
        {
            using var db = GetConnection();
            db.Open();
            var query = @"DELETE FROM users WHERE id = :id";

            await db.ExecuteAsync(query, new {id = userId});

            await DeleteEvents("users", userId);
        }
    }
}