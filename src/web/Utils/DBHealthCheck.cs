using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace web.Utils
{
    public class DBHealthCheck : IHealthCheck
    {
        private storage.postgres.AggregateStorage _storage;

        public DBHealthCheck(storage.postgres.AggregateStorage storage)
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