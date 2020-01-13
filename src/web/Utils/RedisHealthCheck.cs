using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace web.Utils
{
    public class RedisHealthCheck : IHealthCheck
    {
        private storage.redis.AggregateStorage _storage;

        public RedisHealthCheck(storage.redis.AggregateStorage storage)
        {
            _storage = storage;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            await _storage.DoHealthCheck();

            return HealthCheckResult.Healthy();
        }
    }
}