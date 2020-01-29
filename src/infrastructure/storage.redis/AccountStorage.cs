using System.Threading.Tasks;
using core.Account;

namespace storage.redis
{
    public class AccountStorage : AggregateStorage, IAccountStorage
    {
        private const string USER_ENTITY = "users";
        private const string USER_RECORDS_KEY = "userrecords";

        public AccountStorage(string redisCnn) : base(redisCnn)
        {
        }

        public async Task<User> GetUser(string emailAddress)
        {
            var db = _redis.GetDatabase();
            
            var id = await db.HashGetAsync(USER_RECORDS_KEY, emailAddress);
            if (id.IsNullOrEmpty)
            {
                return null;
            }

            var events = await GetEventsAsync(USER_ENTITY, id);

            return new User(events);
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
    }
}