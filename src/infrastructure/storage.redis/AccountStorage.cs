using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using MediatR;
using Newtonsoft.Json;

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

            await _mediator.Publish(new UserStatusRecalculate(userId));
        }

        public async Task Delete(User user)
        {
            var db = _redis.GetDatabase();

            await db.HashDeleteAsync(USER_RECORDS_KEY, user.State.Email);

            await DeleteAggregates(USER_ENTITY, user.Id);
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

        public async Task<IEnumerable<(string, string)>> GetUserEmailIdPairs()
        {
            var db = _redis.GetDatabase();

            var id = await db.HashGetAllAsync(USER_RECORDS_KEY);

            return id.Select(i => (i.Name.ToString(), i.Value.ToString()));
        }

        public async Task<T> ViewModel<T>(Guid userId)
        {
            var db = _redis.GetDatabase();

            var key = typeof(T).Name + ":" + userId;

            var json = await db.StringGetAsync(key);

            return json.IsNull ? default(T) : JsonConvert.DeserializeObject<T>(json);
        }

        public async Task SaveViewModel<T>(T user, Guid userId)
        {
            var db = _redis.GetDatabase();

            var key = typeof(T).Name + ":" + userId;

            await db.StringSetAsync(key, JsonConvert.SerializeObject(user));
        }
    }
}