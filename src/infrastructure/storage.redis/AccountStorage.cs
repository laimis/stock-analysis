using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using Newtonsoft.Json;

namespace storage.redis
{
    public class AccountStorage : AggregateStorage, IAccountStorage
    {
        public AccountStorage(string redisCnn) : base(redisCnn)
        {
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