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
    }
}