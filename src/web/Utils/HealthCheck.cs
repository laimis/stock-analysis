using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using storage.shared;

namespace web.Utils
{
    public class HealthCheck : IHealthCheck
    {
        private IAggregateStorage _storage;

        public HealthCheck(IAggregateStorage storage)
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