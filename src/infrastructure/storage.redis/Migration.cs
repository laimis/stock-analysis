using System;
using System.Linq;
using System.Threading.Tasks;
using core.Options;
using StackExchange.Redis;

namespace storage.redis
{
    public class Migration
    {
        protected ConnectionMultiplexer _redis;

        public Migration(string redisCnn)
        {
            _redis = ConnectionMultiplexer.Connect(redisCnn);
        }

        public async Task<int> FixMistakenEntry()
        {
            const string entity = "soldoption";
            const string userId = "laimis@gmail.com";

            var redisKey = entity + ":" + userId;

            var db = _redis.GetDatabase();

            var keys = await db.SetMembersAsync(redisKey);

            var events = keys.Select(async k => await db.HashGetAllAsync(k.ToString()))
                .Select(e => AggregateStorage.ToEvent(entity, userId, e.Result));
            
            var fixedRecords = 0;

            foreach(var e in events)
            {
                if (e.Event is OptionPurchased c && c.Premium == 140 && e.Key.Contains("WORK"))
                {
                    // c.Premium = 40;

                    var fields = new HashEntry[] {
                        new HashEntry("created", e.Created.ToString("o")),
                        new HashEntry("entity", entity),
                        new HashEntry("event", e.EventJson),
                        new HashEntry("key", e.Key),
                        new HashEntry("userId", e.UserId),
                        new HashEntry("version", e.Version),
                    };

                    var keyToStore = $"{entity}:{e.UserId}:{e.Key}:{e.Version}";

                    await db.HashSetAsync(keyToStore, fields);

                    fixedRecords++;
                }
            }

            return fixedRecords;
        }
    }
}