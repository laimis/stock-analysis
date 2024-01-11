using System;
using System.Threading;
using System.Threading.Tasks;
using core.fs.Accounts;
using core.fs.Alerts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using web.Utils;

namespace web.BackgroundServices;

public abstract class GenericBackgroundServiceHost(core.fs.Adapters.Logging.ILogger logger) : BackgroundService
{
    // ReSharper disable once InconsistentNaming
    protected abstract DateTimeOffset GetNextRunDateTime(DateTimeOffset referenceTime);
    protected core.fs.Adapters.Logging.ILogger baseLogger => logger;

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

public class StopLossServiceHost(
    ILogger<StopLossServiceHost> logger,
    MonitoringServices.StopLossMonitoringService stopLossMonitoringService)
    : GenericBackgroundServiceHost(new WrappingLogger(logger))
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => stopLossMonitoringService.NextRunTime(now);

    protected override async Task Loop(core.fs.Adapters.Logging.ILogger logger, CancellationToken stoppingToken) => await stopLossMonitoringService.Execute(logger, stoppingToken);
}

public class PatternMonitoringServiceHost(
    ILogger<PatternMonitoringServiceHost> logger,
    MonitoringServices.PatternMonitoringService patternMonitoringService)
    : GenericBackgroundServiceHost(new WrappingLogger(logger))
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => patternMonitoringService.NextRunTime(now);

    protected override Task Loop(core.fs.Adapters.Logging.ILogger logger, CancellationToken stoppingToken) => patternMonitoringService.Execute(logger, stoppingToken);
}

public class WeeklyUpsideReversalServiceHost(
    ILogger<MonitoringServices.WeeklyUpsideMonitoringService> logger,
    MonitoringServices.WeeklyUpsideMonitoringService service)
    : GenericBackgroundServiceHost(new WrappingLogger(logger))
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => service.NextRunTime(now);

    protected override async Task Loop(core.fs.Adapters.Logging.ILogger logger, CancellationToken stoppingToken) => await service.Execute(logger, stoppingToken);
}

public class BrokerageServiceHost(ILogger<BrokerageServiceHost> logger, RefreshBrokerageConnectionService service)
    : GenericBackgroundServiceHost(new WrappingLogger(logger))
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => service.NextRunTime(now);

    protected override Task Loop(core.fs.Adapters.Logging.ILogger logger, CancellationToken stoppingToken) => service.Execute(logger, stoppingToken);
}

public class AlertEmailServiceHost(ILogger<MonitoringServices.AlertEmailService> logger, MonitoringServices.AlertEmailService service)
    : GenericBackgroundServiceHost(new WrappingLogger(logger))
{
    private bool _firstRun = true;
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => service.NextRunTime(baseLogger, now);

    protected override async Task Loop(core.fs.Adapters.Logging.ILogger logger, CancellationToken stoppingToken)
    {
        if (_firstRun)
        {
            _firstRun = false;
            logger.LogInformation("First run for alert emailing, sleeping for 5 minutes");
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        
        await service.Execute(logger, stoppingToken);
    }
}

public class ThirtyDaySellServiceHost(
    ILogger<core.fs.Portfolio.MonitoringServices.ThirtyDaySellService> logger,
    core.fs.Portfolio.MonitoringServices.ThirtyDaySellService service) 
    : GenericBackgroundServiceHost(new WrappingLogger(logger))
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => service.NextRun(now);
    
    protected override Task Loop(core.fs.Adapters.Logging.ILogger logger, CancellationToken stoppingToken) => service.Execute(logger, stoppingToken);
}

public class BrokerageAccountServiceHost(
    ILogger<core.fs.Brokerage.MonitoringServices.AccountMonitoringService> logger,
    core.fs.Brokerage.MonitoringServices.AccountMonitoringService service)
    : GenericBackgroundServiceHost(new WrappingLogger(logger))
{
    protected override DateTimeOffset GetNextRunDateTime(DateTimeOffset now) => service.NextRun(now);

    protected override Task Loop(core.fs.Adapters.Logging.ILogger logger, CancellationToken stoppingToken) => service.Execute(logger, stoppingToken);
}