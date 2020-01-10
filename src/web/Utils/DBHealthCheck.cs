using System.Threading;
using System.Threading.Tasks;
using core.Portfolio;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace web.Utils
{
    public class DBHealthCheck : IHealthCheck
    {
        private IPortfolioStorage _storage;

        public DBHealthCheck(IPortfolioStorage storage)
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