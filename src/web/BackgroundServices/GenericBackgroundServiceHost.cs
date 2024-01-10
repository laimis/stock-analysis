using System;
using System.Threading;
using System.Threading.Tasks;
using core.fs.Accounts;
using core.fs.Alerts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices;

public abstract class GenericBackgroundServiceHost(ILogger logger) : BackgroundService
{
    // ReSharper disable once InconsistentNaming
    protected abstract DateTimeOffset GetNextRunDateTime(DateTimeOffset referenceTime);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // warm up sleep duration, generate random seconds from 5 to 10 seconds
        var randomSleep = new Random().Next(5, 10);
        await Task.Delay(TimeSpan.FromSeconds(randomSleep), stoppingToken);
        
        logger.LogInformation("running {name}", GetType().Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Loop(stoppingToken);
            }
            catch(Exception ex)
            {
                logger.LogError("Failed: {exception}", ex);
            }

            var sleepDuration = GetNextRunDateTime(DateTimeOffset.UtcNow).Subtract(DateTimeOffset.UtcNow);
            if (sleepDuration.TotalMinutes > 1) // only show less frequent sleeps in order not to spam logs
            {
                logger.LogInformation("sleeping for {sleepDuration}", sleepDuration);
            }
            if (sleepDuration.TotalMinutes < 0)
            {
                logger.LogWarning("sleep duration is negative: {sleepDuration}", sleepDuration);
            }

            await Task.Delay(sleepDuration, stoppingToken);
        }

        logger.LogInformation("{name} exit", GetType().Name);
    }

    protected abstract Task Loop(CancellationToken stoppingToken);
}

public class StopLossServiceHost(
    ILogger<StopLossServiceHost> logger,
    MonitoringServices.StopLossMonitoringService stopLossMonitoringService)
    : GenericBackgroundServiceHost(logger)
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => stopLossMonitoringService.NextRunTime(now);

    protected override async Task Loop(CancellationToken stoppingToken) => await stopLossMonitoringService.Execute(stoppingToken);
}

public class PatternMonitoringServiceHost(
    ILogger<PatternMonitoringServiceHost> logger,
    MonitoringServices.PatternMonitoringService patternMonitoringService)
    : GenericBackgroundServiceHost(logger)
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => patternMonitoringService.NextRunTime(now);

    protected override Task Loop(CancellationToken stoppingToken) => patternMonitoringService.Execute(stoppingToken);
}

public class WeeklyUpsideReversalServiceHost(
    ILogger<MonitoringServices.WeeklyUpsideMonitoringService> logger,
    MonitoringServices.WeeklyUpsideMonitoringService service)
    : GenericBackgroundServiceHost(logger)
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => service.NextRunTime(now);

    protected override async Task Loop(CancellationToken stoppingToken) => await service.Execute(stoppingToken);
}

public class BrokerageServiceHost(ILogger<BrokerageServiceHost> logger, RefreshBrokerageConnectionService service)
    : GenericBackgroundServiceHost(logger)
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => service.NextRunTime(now);

    protected override Task Loop(CancellationToken stoppingToken) => service.Execute(stoppingToken);
}

public class AlertEmailServiceHost(ILogger<MonitoringServices.AlertEmailService> logger, MonitoringServices.AlertEmailService service):
    GenericBackgroundServiceHost(logger)
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => service.NextRunTime(now);

    protected override Task Loop(CancellationToken stoppingToken) => service.Execute(stoppingToken);
}

public class ThirtyDaySellServiceHost(
    ILogger<core.fs.Portfolio.MonitoringServices.ThirtyDaySellService> logger,
    core.fs.Portfolio.MonitoringServices.ThirtyDaySellService service) : GenericBackgroundServiceHost(logger)
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => service.NextRun(now);
    
    protected override Task Loop(CancellationToken stoppingToken) => service.Execute(stoppingToken);
}