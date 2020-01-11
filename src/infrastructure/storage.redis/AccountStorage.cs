using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace storage.redis
{
    public class AccountStorage : IAccountStorage
    {
        private ConnectionMultiplexer _redis;

        public AccountStorage(string redisCnn)
        {
            _redis = ConnectionMultiplexer.Connect(redisCnn);
        }

        public async Task<IEnumerable<LoginLogEntry>> GetLogins()
        {
            var db = _redis.GetDatabase();

            var list = await db.ListRangeAsync("loggedinusers");

            return list.Select(v => JsonConvert.DeserializeObject<LoginLogEntry>(v));
        }

        public async Task RecordLoginAsync(LoginLogEntry entry)
        {
            var db = _redis.GetDatabase();

            await db.ListLeftPushAsync("loggedinusers", JsonConvert.SerializeObject(entry));
        }
    }
}