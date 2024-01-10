using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.fs;
using core.fs.Accounts;
using core.fs.Adapters.Email;
using core.fs.Adapters.Storage;
using core.fs.Alerts;
using core.fs.Reports;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Handler = core.fs.Reports.Handler;

namespace web.BackgroundServices;

public abstract class GenericBackgroundServiceHost(ILogger logger) : BackgroundService
{
    // ReSharper disable once InconsistentNaming
    protected readonly ILogger _logger = logger;

    protected abstract TimeSpan GetSleepDuration();

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

            var sleepDuration = GetSleepDuration();
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

public class StopLossServiceHost(
    ILogger<StopLossServiceHost> logger,
    MonitoringServices.StopLossMonitoringService stopLossMonitoringService)
    : GenericBackgroundServiceHost(logger)
{
    protected override TimeSpan GetSleepDuration() =>
        stopLossMonitoringService.NextRunTime(DateTimeOffset.UtcNow) - DateTimeOffset.UtcNow;

    protected override async Task Loop(CancellationToken stoppingToken) => await stopLossMonitoringService.Execute(stoppingToken);
}

public class PatternMonitoringServiceHost(
    ILogger<PatternMonitoringServiceHost> logger,
    MonitoringServices.PatternMonitoringService patternMonitoringService)
    : GenericBackgroundServiceHost(logger)
{
    protected override TimeSpan GetSleepDuration() =>
        patternMonitoringService.NextRunTime(DateTimeOffset.UtcNow) - DateTimeOffset.UtcNow;

    protected override async Task Loop(CancellationToken stoppingToken) => await patternMonitoringService.Execute(stoppingToken);
}

public class WeeklyUpsideReversalServiceHost(
    ILogger<MonitoringServices.WeeklyUpsideMonitoringService> logger,
    MonitoringServices.WeeklyUpsideMonitoringService service)
    : GenericBackgroundServiceHost(logger)
{
    protected override TimeSpan GetSleepDuration() =>
        service.NextRunTime(DateTimeOffset.UtcNow) - DateTimeOffset.UtcNow;

    protected override async Task Loop(CancellationToken stoppingToken) => await service.Execute(stoppingToken);
}

public class BrokerageServiceHost(ILogger<BrokerageServiceHost> logger, RefreshBrokerageConnectionService service)
    : GenericBackgroundServiceHost(logger)
{
    protected override TimeSpan GetSleepDuration() => TimeSpan.FromHours(12);

    protected override Task Loop(CancellationToken stoppingToken)
    {
        return service.Execute(stoppingToken);
    }
}

public class AlertEmailServiceHost(ILogger<AlertEmailService> logger, AlertEmailService service):
    GenericBackgroundServiceHost(logger)
{
    protected override TimeSpan GetSleepDuration() => service.NextRunTime(DateTimeOffset.UtcNow);

    protected override Task Loop(CancellationToken stoppingToken) => service.Execute(stoppingToken);
}

public class ThirtyDaySellService(
    ILogger<ThirtyDaySellService> logger,
    IAccountStorage accounts,
    IEmailService emails,
    Handler service)
    : GenericBackgroundServiceHost(logger)
{
    private static readonly TimeSpan _sleepInterval = TimeSpan.FromHours(24);
    protected override TimeSpan GetSleepDuration() => _sleepInterval;

    protected override async Task Loop(CancellationToken stoppingToken)
    {
        var pairs = await accounts.GetUserEmailIdPairs();

        foreach(var p in pairs)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessUser(p);
            }
            catch(Exception ex)
            {
                _logger.LogError("Failed to process 30 day check for {email}: {exception}", p.Email, ex);
            }
        }
    }

    private async Task ProcessUser(EmailIdPair p)
    {
        // 30 day crosser
        _logger.LogInformation("Scanning {email}", p.Email);

        var query = new SellsQuery(p.Id);

        var sellView = await service.Handle(query);

        if (sellView.IsOk == false)
        {
            _logger.LogError("Failed to get sells for {email}: {error}", p.Email, sellView.Error.Value.Message);
            return;
        }
        
        var sellsOfInterest = sellView.Success.Value.Sells.Where(s => s.Age.Days is >= 27 and <= 31).ToList();

        if (sellsOfInterest.Count > 0)
        {
            await emails.SendWithTemplate(
                recipient: new Recipient(email: p.Email, name: null),
                Sender.NoReply,
                template: EmailTemplate.SellAlert,
                new { sells = sellsOfInterest }
            );
        }
        else
        {
            _logger.LogInformation("No sells of interest for {email}", p.Email);
        }
    }
}