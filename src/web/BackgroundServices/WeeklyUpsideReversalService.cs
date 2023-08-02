using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Adapters.Emails;
using core.Shared.Adapters.Brokerage;
using core.Stocks.Services.Analysis;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices;

public class WeeklyUpsideReversalService : GenericBackgroundServiceHost
{
    private readonly IAccountStorage _accounts;
    private readonly IBrokerage _brokerage;
    private readonly IPortfolioStorage _portfolioStorage;
    private readonly IEmailService _emails;
    private readonly IMarketHours _marketHours;

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
    }

    protected override TimeSpan SleepDuration
    {
        get {
            // this report goes out every Friday at 5:00 PM ET
            var nowInMarketHours = _marketHours.ToMarketTime(DateTimeOffset.UtcNow);
            var nextFriday = nowInMarketHours.AddDays(5 - (int)nowInMarketHours.DayOfWeek);
            var nextFriday5pm = nextFriday.Date.AddHours(17);
            var sleepDuration = nextFriday5pm - nowInMarketHours;
            return sleepDuration;
        }
    }

    protected override async Task Loop(CancellationToken stoppingToken)
    {
        var pairs = await _accounts.GetUserEmailIdPairs();

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
                _logger.LogError("Failed to process weekly upsde check {email}: {exception}", p.email, ex);
            }
        }
    }

    private async Task ProcessUser((string email, string id) p)
    {
        var user = await _accounts.GetUserByEmail(p.email);
        if (user == null)
        {
            _logger.LogError("User not found for {email}", p.email);
            return;
        }

        _logger.LogInformation("Processing user {email} upsides", p.email);

        var stocks = await _portfolioStorage.GetStocks(user.Id);
        var positions = stocks.Where(s => s.State.OpenPosition != null).Select(s => s.State.OpenPosition).ToList();

        var detected = new List<(string Ticker, Pattern Pattern)>(); 

        foreach(var position in positions)
        {
            var priceBars = await _brokerage.GetPriceHistory(user.State, position.Ticker, core.Shared.Adapters.Stocks.PriceFrequency.Weekly);
            if (!priceBars.IsOk)
            {
                _logger.LogError("Unabel to get price bars for {ticker} with error {error}", position.Ticker, priceBars.Error);
                continue;
            }

            var pattern = PatternDetection.UpsideReversal(priceBars.Success);
            if (pattern == null)
            {
                _logger.LogInformation("No upside reversal found for {ticker}", position.Ticker);
                continue;
            }

            detected.Add((position.Ticker, pattern.Value));
        }

        if (detected.Count == 0)
        {
            _logger.LogInformation("No upside reversals found");
        }
        else
        {
            var alertGroups = new List<object> {
                new {
                    identifier = "Weekly Upside Reversals",
                    alerts = detected.Select(d => StockAlertService.ToEmailRow(
                        valueType: d.Pattern.valueFormat,
                        triggeredValue: d.Pattern.value,
                        ticker: d.Ticker,
                        description: "Weekly Upside Reversal",
                        sourceList: "Portfolio",
                        time: _marketHours.ToMarketTime(d.Pattern.date)
                    ))
                }
            };

            var data = new { alertGroups };
            await _emails.Send(
                    recipient: new Recipient(email: p.email, name: null),
                    Sender.NoReply,
                    template: EmailTemplate.Alerts,
                    data
                );
        }
    }
}
