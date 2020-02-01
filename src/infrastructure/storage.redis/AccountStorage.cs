using System.Threading.Tasks;
using core.Account;

namespace storage.redis
{
    public class AccountStorage : RedisAggregateStorage, IAccountStorage
    {
        private const string USER_ENTITY = "users";
        private const string USER_RECORDS_KEY = "userrecords";

        public AccountStorage(string redisCnn) : base(redisCnn)
        {
        }

        public async Task<User> GetUser(string userId)
        {
            var events = await GetEventsAsync(USER_ENTITY, userId);

            var u = new User(events);
            if (u.Id == System.Guid.Empty)
            {
                return null;
            }
            return u;
        }

        public async Task<User> GetUserByEmail(string emailAddress)
        {
            emailAddress = emailAddress.ToLowerInvariant();

            var db = _redis.GetDatabase();

            var id = await db.HashGetAsync(USER_RECORDS_KEY, emailAddress);
            if (id.IsNullOrEmpty)
            {
                return null;
            }

            return await GetUser(id);
        }

        public async Task Save(User u)
        {
            var db = _redis.GetDatabase();

            var userId = u.State.Id.ToString();

            await db.HashSetAsync(
                USER_RECORDS_KEY,
                u.State.Email,
                userId,
                StackExchange.Redis.When.NotExists);

            await SaveEventsAsync(u, USER_ENTITY, userId);
        }

        public async Task Delete(User user)
        {
            var db = _redis.GetDatabase();

            await db.HashDeleteAsync(USER_RECORDS_KEY, user.State.Email);

            await DeleteEvents(USER_ENTITY, user.Id.ToString());
        }
    }
}