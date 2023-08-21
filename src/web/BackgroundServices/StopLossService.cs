using System;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Alerts;
using core.Alerts.Services;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices;
public class StopLossService : GenericBackgroundServiceHost
{
    private IAccountStorage _accounts;
    private IBrokerage _brokerage;
    private StockAlertContainer _container;
    private IMarketHours _marketHours;
    private IPortfolioStorage _portfolioStorage;

    public StopLossService(
        IAccountStorage accounts,
        IBrokerage brokerage,
        StockAlertContainer container,
        ILogger<StopLossService> logger,
        IMarketHours marketHours,
        IPortfolioStorage portfolioStorage) : base(logger)
    {
        _accounts = accounts;
        _brokerage = brokerage;
        _container = container;
        _marketHours = marketHours;
        _portfolioStorage = portfolioStorage;
    }

    // stop loss should be monitored at the following times:
    // on trading days every 5 minutes from 9:45am to 3:30pm
    // and no monitoring on weekends

    private static readonly TimeOnly _marketStartTime = new TimeOnly(9, 30, 0);
    private static readonly TimeOnly _marketEndTime = new TimeOnly(16, 0, 0);

    protected override TimeSpan GetSleepDuration()
    {
        var now = DateTimeOffset.UtcNow;
        var eastern = _marketHours.ToMarketTime(now);
        var marketStartTimeInEastern = eastern.Date.Add(_marketStartTime.ToTimeSpan());

        var nextScan = TimeOnly.FromTimeSpan(eastern.TimeOfDay) switch {
            var t when t < _marketStartTime => marketStartTimeInEastern,
            var t when t > _marketEndTime => marketStartTimeInEastern.AddDays(1),
            _ => eastern.AddMinutes(5)
        };

        // if the next scan is on a weekend, let's skip those days
        if (nextScan.DayOfWeek == DayOfWeek.Saturday)
        {
            nextScan = nextScan.AddDays(2);
        }
        else if (nextScan.DayOfWeek == DayOfWeek.Sunday)
        {
            nextScan = nextScan.AddDays(1);
        }

        var nextRun = _marketHours.ToUniversalTime(nextScan);

        return nextRun - now;
    }

    protected override async Task Loop(CancellationToken stoppingToken)
    {
        _container.ToggleStopLossCheckCompleted(false);

        var users = await _accounts.GetUserEmailIdPairs();

        foreach(var (_, userId) in users)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            _logger.LogInformation($"Running stop loss check for {userId}");

            var user = await _accounts.GetUser(new Guid(userId));
            if (user == null)
            {
                _logger.LogError($"Unable to find user {userId}");
                continue;
            }

            var checks = await AlertCheckGenerator.GetStopLossChecks(_portfolioStorage, user.State);

            var completed = await core.Alerts.Services.Monitors.MonitorForStopLosses(
                quoteFunc: GetQuote,
                container: _container,
                checks: checks,
                cancellationToken: stoppingToken
            );
            
            // only update the next run time if we've completed all checks
            // TODO: I don't think I am handling this properly
            if (completed.Count != checks.Count)
            {
                _logger.LogInformation($"Not all stop loss checks completed for {userId}");
                _container.AddNotice($"Not all stop loss checks completed for {userId}");
            }
        }

        _container.ToggleStopLossCheckCompleted(true);
    }

    private async Task<ServiceResponse<StockQuote>> GetQuote(
            UserState user, string ticker)
    {
        var priceResponse = await _brokerage.GetQuote(user, ticker);
        if (!priceResponse.IsOk)
        {
            _logger.LogError("Could not get price for {ticker}: {message}", ticker, priceResponse.Error.Message);
        }
        return priceResponse;
    }
}
