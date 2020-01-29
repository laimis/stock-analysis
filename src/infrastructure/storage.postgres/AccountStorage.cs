using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using Dapper;

namespace storage.postgres
{
    public class AccountStorage : AggregateStorage, IAccountStorage
    {
        private const string _user_entity = "users";

        public AccountStorage(string cnn) : base(cnn)
        {
        }

        public async Task<User> GetUser(string emailAddress)
        {
            using var db = GetConnection();
            db.Open();
            var query = @"SELECT id FROM users WHERE email = :emailAddress";

            var identifier = await db.QuerySingleOrDefaultAsync<string>(query, new {emailAddress});
            if (identifier == null)
            {
                return null;
            }

            var events = await GetEventsAsync(_user_entity, identifier);

            return new User(events);
        }

        public async Task Save(User u)
        {
            using var db = GetConnection();
            db.Open();
            var query = @"INSERT INTO users (id, email) VALUES (:id, :email) ON CONFLICT DO NOTHING;";

            await db.ExecuteAsync(query, new {id = u.State.Id.ToString(), email = u.State.Email});

            await SaveEventsAsync(u, _user_entity, u.State.Id.ToString());
        }
    }
}