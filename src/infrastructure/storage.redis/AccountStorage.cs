using System;
using System.Threading.Tasks;
using core.Account;
using MediatR;

namespace storage.redis
{
    public class AccountStorage : RedisAggregateStorage, IAccountStorage
    {
        private const string USER_ENTITY = "users";
        private const string USER_RECORDS_KEY = "userrecords";

        public AccountStorage(IMediator mediator, string redisCnn) : base(mediator, redisCnn)
        {
        }

        public async Task<User> GetUser(Guid userId)
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

            return await GetUser(new Guid(id.ToString()));
        }

        public async Task Save(User u)
        {
            var db = _redis.GetDatabase();

            var userId = u.State.Id;

            await db.HashSetAsync(
                USER_RECORDS_KEY,
                u.State.Email,
                userId.ToString(),
                StackExchange.Redis.When.NotExists);

            await SaveEventsAsync(u, USER_ENTITY, userId);
        }

        public async Task Delete(User user)
        {
            var db = _redis.GetDatabase();

            await db.HashDeleteAsync(USER_RECORDS_KEY, user.State.Email);

            await DeleteEvents(USER_ENTITY, user.Id);
        }

        public async Task SaveUserAssociation(ProcessIdToUserAssociation r)
        {
            var db = _redis.GetDatabase();

            await db.HashSetAsync(
                "passwordreset:" + r.Id,
                new StackExchange.Redis.HashEntry[] {
                    new StackExchange.Redis.HashEntry("id", r.Id.ToString()),
                    new StackExchange.Redis.HashEntry("userid", r.UserId.ToString()),
                    new StackExchange.Redis.HashEntry("timestamp", r.Timestamp.ToString()),
                }
            );
        }

        public async Task<ProcessIdToUserAssociation> GetUserAssociation(Guid id)
        {
            var db = _redis.GetDatabase();

            var entries = await db.HashGetAllAsync("passwordreset:" + id);

            Guid userId; string timestamp = null;

            foreach(var e in entries)
            {
                switch (e.Name)
                {
                    case "userid":
                        userId = Guid.Parse(e.Value);
                        break;
                    case "timestamp":
                        timestamp = e.Value;
                        break;
                    default:
                        break;
                }
            }

            if (userId == Guid.Empty)
            {
                return null;
            }

            return new ProcessIdToUserAssociation(id, userId, timestamp);
        }
    }
}