using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace web.BackgroundServices;

public abstract class GenericBackgroundServiceHost(core.fs.Adapters.Logging.ILogger logger) : BackgroundService
{
    // ReSharper disable once InconsistentNaming
    protected abstract DateTimeOffset GetNextRunDateTime(DateTimeOffset referenceTime);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // warm up sleep duration, generate random seconds from 5 to 10 seconds
        var randomSleep = new Random().Next(5, 10);
        await Task.Delay(TimeSpan.FromSeconds(randomSleep), stoppingToken);
        
        logger.LogInformation($"running {GetType().Name}");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Loop(logger, stoppingToken);
            }
            catch(Exception ex)
            {
                logger.LogError($"Failed: {ex}");
            }

            var now = DateTimeOffset.UtcNow;
            var nextRun = GetNextRunDateTime(now);
            var sleepDuration = nextRun.Subtract(now);
            
            switch (sleepDuration.TotalMinutes)
            {
                // only show less frequent sleeps in order not to spam logs
                case > 1:
                    logger.LogInformation($"Next run {nextRun:u}, sleeping for {sleepDuration}");
                    break;
                case < 0:
                    logger.LogWarning($"sleep duration is negative: {sleepDuration}");
                    break;
            }

            await Task.Delay(sleepDuration, stoppingToken);
        }

        logger.LogInformation($"{GetType().Name} exit");
    }

    protected abstract Task Loop(core.fs.Adapters.Logging.ILogger logger, CancellationToken stoppingToken);
}
