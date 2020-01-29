using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using Newtonsoft.Json;

namespace storage.redis
{
    public class AccountStorage : AggregateStorage, IAccountStorage
    {
        private const string _user_entity = "users";

        public AccountStorage(string redisCnn) : base(redisCnn)
        {
        }

        public async Task<User> GetUser(string emailAddress)
        {
            // first you need to check if email has user id
            var db = _redis.GetDatabase();

            var id = await db.StringGetAsync(emailAddress);
            if (id.IsNullOrEmpty)
            {
                return null;
            }

            var events = await GetEventsAsync(_user_entity, id);

            return new User(events);
        }

        public async Task Save(User u)
        {
            var db = _redis.GetDatabase();

            var userId = u.State.Id.ToString();

            await db.StringSetAsync(u.State.Email, userId);

            await SaveEventsAsync(u, _user_entity, userId);
        }
    }
}