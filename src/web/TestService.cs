using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web
{
    internal class TestService : BackgroundService
    {
        private ILogger<TestService> _logger;

        public TestService(ILogger<TestService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("exec enter");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                // _logger.LogDebug("loop " + DateTime.UtcNow);

                await Task.Delay(1000 * 3, stoppingToken);
            }

            _logger.LogDebug("exec exit");
        }
    }
}