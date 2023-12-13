using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.fs.Alerts;
using core.fs.Services;
using core.fs.Shared.Adapters.Brokerage;
using core.fs.Shared.Adapters.Email;
using core.fs.Shared.Adapters.Stocks;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
using core.Shared;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices;

/// <summary>
/// This service operates in three phases:
/// 1. LoadTickersToCheck - it starts with loading all the users in the system and their portfolios and stock lists
/// this forms the universe of tickers to check for weekly upside reversals.
/// 2. CheckTickers - once #1 is finished, it goes through the list of tickers and checks if they have weekly upside reversal
/// 3. SendEmails - once #2 is finished, it goes through the list of users and sends them emails with tickers that have weekly upside reversal
/// </summary>
public class WeeklyUpsideReversalService : GenericBackgroundServiceHost
{
    private readonly IAccountStorage _accounts;
    private readonly IBrokerage _brokerage;
    private readonly IPortfolioStorage _portfolioStorage;
    private readonly IEmailService _emails;
    private readonly IMarketHours _marketHours;

    private readonly Dictionary<UserState, HashSet<Ticker>> _tickersToCheck = new();
    private readonly Dictionary<UserState, List<TriggeredAlert>> _weeklyUpsidesDiscovered = new();

    private Func<CancellationToken, Task> _toRun;
    private Func<TimeSpan> _sleepCalculation;

    public WeeklyUpsideReversalService(
        ILogger<WeeklyUpsideReversalService> logger,
        IAccountStorage accounts,
        IBrokerage brokerage,
        IEmailService emails,
        IPortfolioStorage portfolioStorage,
        IMarketHours marketHours) : base(logger)
    {
        _accounts = accounts;
        _brokerage = brokerage;
        _emails = emails;
        _portfolioStorage = portfolioStorage;
        _marketHours = marketHours;
        _toRun = LoadTickersToCheck;
        _sleepCalculation = AfterMarketCloseOnFriday;
    }

    protected override TimeSpan GetSleepDuration() => _sleepCalculation();
    
    
    TimeSpan AfterMarketCloseOnFriday() {
        var nowInMarketHours = _marketHours.ToMarketTime(DateTimeOffset.UtcNow);
        var nextFriday = nowInMarketHours.AddDays(5 - (int)nowInMarketHours.DayOfWeek);
        // ReSharper disable once InconsistentNaming
        var nextFriday5PM = nextFriday.Date.AddHours(17);
        var sleepDuration = nextFriday5PM - nowInMarketHours;
        return sleepDuration;
    }

    static TimeSpan WhileCheckIsInProgress() => TimeSpan.FromSeconds(30);

    protected override Task Loop(CancellationToken stoppingToken) => _toRun(stoppingToken);

    private async Task LoadTickersToCheck(CancellationToken stoppingToken)
    {
        bool FridayOrWeekend()
        {
            return _marketHours.ToMarketTime(DateTimeOffset.UtcNow).DayOfWeek switch {
                DayOfWeek.Friday => true,
                DayOfWeek.Saturday => true,
                DayOfWeek.Sunday => true,
                _ => false
            };
        }

        // if it's not Friday or weekend, then we don't need to do anything
        if (!FridayOrWeekend())
        {
            _logger.LogInformation("Not a Friday/Weekend, skipping weekly upside check");

            _toRun = LoadTickersToCheck;
            _sleepCalculation = AfterMarketCloseOnFriday;
            return;
        }

        var pairs = await _accounts.GetUserEmailIdPairs();

        _tickersToCheck.Clear();

        foreach (var emailId in pairs)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var userOption = await _accounts.GetUserByEmail(emailId.Email);
                if (userOption == null)
                {
                    _logger.LogError("User not found for {email}", emailId.Email);
                    continue;
                }
                
                var user = userOption.Value;

                _logger.LogInformation("Processing user {email} upsides", emailId.Email);
                
                var userId = UserId.NewUserId(user.Id);
                var stocks = await _portfolioStorage.GetStockPositions(userId);
                var tickersFromPositions = stocks.Where(s => s.IsOpen).Select(s => s.Ticker);
                var tickersFromLists = (await _portfolioStorage.GetStockLists(userId))
                    .Where(l => l.State.ContainsTag(Constants.MonitorTagPattern))
                    .SelectMany(l => l.State.Tickers)
                    .Select(t => t.Ticker);

                var set = new HashSet<Ticker>(tickersFromLists.Union(tickersFromPositions));

                _tickersToCheck[user.State] = set;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to process weekly upside check {email}: {exception}", emailId.Email, ex);
            }
        }

        _toRun = RunWeeklyUpsideCheck;
        _sleepCalculation = WhileCheckIsInProgress;
    }

    private async Task RunWeeklyUpsideCheck(CancellationToken token)
    {
        _logger.LogInformation("Running weekly upside check for {count} users", _tickersToCheck.Count);

        foreach(var u in _tickersToCheck)
        {
            var tickersToCheck = u.Value.ToList();

            foreach(var ticker in tickersToCheck)
            {
                var priceBars = await _brokerage.GetPriceHistory(u.Key, ticker, PriceFrequency.Weekly, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
                if (!priceBars.IsOk)
                {
                    _logger.LogError("Unable to get price bars for {ticker} with error {error}", ticker, priceBars.Error.Value);
                    continue;
                }

                var pattern = PatternDetection.upsideReversal(priceBars.Success.Value);

                // this is important, we need to remove the check from the tickers to check list while adding in to 
                // upside reversal list because midway through this process broker will throw exception most likely 
                // due to price throttling and we will never complete this process otherwise
                u.Value.Remove(ticker);

                if (pattern == null)
                {
                    _logger.LogInformation("No upside reversal found for {ticker}", ticker);
                    continue;
                }

                var alert = TriggeredAlert.PatternAlert(
                    pattern.Value, ticker, "Watchlist", DateTimeOffset.UtcNow, UserId.NewUserId(u.Key.Id));

                if (!_weeklyUpsidesDiscovered.ContainsKey(u.Key))
                {
                    _weeklyUpsidesDiscovered[u.Key] = new List<TriggeredAlert>();
                }

                _weeklyUpsidesDiscovered[u.Key].Add(alert);
            }
        }

        _tickersToCheck.Clear();
        _toRun = SendEmails;
        _sleepCalculation = WhileCheckIsInProgress;
    }

    private async Task SendEmails(CancellationToken token)
    {
        _logger.LogInformation("Weekly upside reversal emails discovered for {count} users", _weeklyUpsidesDiscovered.Count);

        foreach(var u in _weeklyUpsidesDiscovered)
        {
            var userState = u.Key;
            var alerts = u.Value;
            
            _logger.LogInformation("Processing {email} with {count} alerts", userState.Email, alerts.Count);

            if (alerts.Count == 0)
            {
                continue;
            }

            await EmailNotificationService.SendAlerts(
                _emails,
                _marketHours,
                new Recipient(email: u.Key.Email, name: u.Key.Name),
                u.Value.GroupBy(_ => "Weekly Upside Reversal")
            );
        }

        _weeklyUpsidesDiscovered.Clear();
        _toRun = LoadTickersToCheck;
        _sleepCalculation = AfterMarketCloseOnFriday;
    }
}
