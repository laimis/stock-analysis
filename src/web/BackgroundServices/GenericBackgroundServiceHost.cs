using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public abstract class GenericBackgroundServiceHost : BackgroundService
    {
        protected readonly ILogger _logger;
        
        public GenericBackgroundServiceHost(ILogger logger)
        {
            _logger = logger;            
        }

        protected abstract TimeSpan SleepDuration { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // warm up sleep duration, generate random seconds from 5 to 10 seconds
            var randomSleep = new Random().Next(5, 10);
            await Task.Delay(TimeSpan.FromSeconds(randomSleep), stoppingToken);
            
            _logger.LogInformation("running {name}", GetType().Name);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Loop(stoppingToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError("Failed: {exception}", ex);
                }

                var sleepDuration = SleepDuration;
                if (sleepDuration.TotalMinutes > 1) // only show less frequent sleeps in order not to spam logs
                {
                    _logger.LogInformation("sleeping for {sleepDuration}", sleepDuration);
                }

                await Task.Delay(sleepDuration, stoppingToken);
            }

            _logger.LogInformation("{name} exit", GetType().Name);
        }

        protected abstract Task Loop(CancellationToken stoppingToken);
    }
}