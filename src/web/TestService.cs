using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web
{
    internal class TestService : IHostedService
    {
        private ILogger<TestService> _logger;

        public TestService(ILogger<TestService> logger)
        {
            _logger = logger;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("start goodness");

            await Task.Run(async () => {
                while(true)
                {
                    _logger.LogInformation("Running service " + DateTime.UtcNow);
                    await Task.Delay(3000);
                }
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("stop goodness");

            return Task.CompletedTask;
        }
    }
}