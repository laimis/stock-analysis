using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Adapters.Emails;
using core.Alerts;
using core.Alerts.Services;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Stocks.Services.Analysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class StockAlertService : BackgroundService
    {
        private IAccountStorage _accounts;
        private IBrokerage _brokerage;
        private StockAlertContainer _container;
        private IEmailService _emails;
        private ILogger<StockAlertService> _logger;
        private IMarketHours _marketHours;
        private IPortfolioStorage _portfolio;

        public StockAlertService(
            IAccountStorage accounts,
            IBrokerage brokerage,
            StockAlertContainer container,
            IEmailService emails,
            ILogger<StockAlertService> logger,
            IMarketHours marketHours,
            IPortfolioStorage portfolio)
        {
            _accounts = accounts;
            _brokerage = brokerage;
            _container = container;
            _emails = emails;
            _logger = logger;
            _marketHours = marketHours;
            _portfolio = portfolio;
        }

        private const string GAP_UP_TAG = "monitor:gapup";
        private const string UPSIDE_REVERSAL_TAG = "monitor:upsidereversal";
        private List<AlertCheck> _gapUpChecks;
        private List<AlertCheck> _upsideReversalChecks;

        private DateTimeOffset _nextAlertUpdateRun = DateTimeOffset.MinValue;
        private DateTimeOffset _nextStopLossCheck = DateTimeOffset.MinValue;
        private DateTimeOffset _nextEmailSend = DateTimeOffset.MinValue;
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {    
                try
                {
                    if (DateTimeOffset.UtcNow > _nextAlertUpdateRun || _container.ManualRunRequested())
                    {
                        var user = await GetUser();
                        if (user == null)
                        {
                            _logger.LogCritical("No user found for stock alert service");
                            _container.AddNotice("No user found for stock alert service");
                            break;
                        }

                        _gapUpChecks = await AlertCheckGenerator.GetStocksFromListsWithTagsAsync(
                            _portfolio, GAP_UP_TAG, user.State
                        );

                        _upsideReversalChecks = await AlertCheckGenerator.GetStocksFromListsWithTagsAsync(
                            _portfolio, UPSIDE_REVERSAL_TAG, user.State
                        );

                        _nextAlertUpdateRun = GetNextMonitorRunTime();

                        _container.ManualRunCompleted();

                        _container.AddNotice(
                            $"Alert check generator added {_gapUpChecks.Count + _upsideReversalChecks.Count} checks, next run at {_marketHours.ToMarketTime(_nextAlertUpdateRun)}"
                        );
                    }

                    if (_gapUpChecks.Count > 0)
                    {
                        var gapUpsCompleted = await MonitorForGaps(_gapUpChecks, stoppingToken);
                        foreach(var completed in gapUpsCompleted)
                        {
                            _gapUpChecks.Remove(completed);
                        }

                        _container.AddNotice($"{gapUpsCompleted.Count} gap up checks completed, {_gapUpChecks.Count} remaining");
                    }

                    if (_upsideReversalChecks.Count > 0)
                    {
                        var upsideReversalsCompleted = await MonitorForUpsideReversals(_upsideReversalChecks, stoppingToken);
                        foreach(var completed in upsideReversalsCompleted)
                        {
                            _upsideReversalChecks.Remove(completed);
                        }
                        
                        _container.AddNotice($"{upsideReversalsCompleted.Count} reversal checks completed, {_upsideReversalChecks.Count} remaining");
                    }

                    if (DateTimeOffset.UtcNow > _nextStopLossCheck)
                    {
                        await MonitorForStops(stoppingToken);
                        _nextStopLossCheck = GetNextStopLossCheckTime();
                        _container.AddNotice("Stop loss monitor complete, next run at " + _marketHours.ToMarketTime(_nextStopLossCheck));
                    }

                    if (DateTimeOffset.UtcNow > _nextEmailSend)
                    {
                        await SendAlertSummaryEmail();
                        _nextEmailSend = GetNextEmailSendTime();
                        _container.AddNotice("Emails sent, next run at " + _marketHours.ToMarketTime(_nextEmailSend));
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed while running alert monitor, will sleep");
                    _container.AddNotice("Failed while running alert monitor: " + ex.Message);
                    await Task.Delay(TimeSpan.FromMinutes(10));
                }
            }
        }

        private async Task<User> GetUser()
        {
            return await _accounts.GetUserByEmail("laimis@gmail.com");
        }

        private DateTimeOffset GetNextEmailSendTime()
        {
            var currentTime = DateTimeOffset.UtcNow;

            var currentTimeInEastern = _marketHours.ToMarketTime(currentTime);
            var oneHourBeforeClose = _marketHours.GetMarketEndOfDayTimeInUtc(currentTimeInEastern)
                .AddHours(-1);
            var afterOpen = _marketHours.GetMarketStartOfDayTimeInUtc(currentTimeInEastern)
                .AddMinutes(15);

            var nextRunTime = 
                currentTime.TimeOfDay switch {
                var t when t >= oneHourBeforeClose.TimeOfDay =>  // after market close
                    afterOpen.AddDays(1),
                var t when t < afterOpen.TimeOfDay => // before market open
                    afterOpen,
                var t when t < oneHourBeforeClose.TimeOfDay => // during market hours
                    oneHourBeforeClose,
                _ =>  throw new Exception("Should not be possible to get here but did with time: " + currentTimeInEastern)
            };

            return nextRunTime;
        }

        private async Task SendAlertSummaryEmail()
        {
            var user = await GetUser();
            if (user == null)
            {
                _logger.LogError("Could not find user for email sending");
                return;
            }

            // get all alerts for that user
            var alerts = _container.GetAlerts(user.Id);

            var data = new {
                alerts = alerts.Select(ToEmailData)
            };

            await _emails.Send(
                new Recipient(email: user.State.Email, name: user.State.Name),
                Sender.NoReply,
                EmailTemplate.Alerts,
                data
            );

        }

        private object ToEmailData(TriggeredAlert alert)
        {
            return new {
                ticker = (string)alert.ticker,
                value = alert.triggeredValue,
                description = alert.description,
                time = _marketHours.ToMarketTime(alert.when).ToString("HH:mm") + " ET"
            };
        }

        private async Task MonitorForStops(CancellationToken stoppingToken)
        {
            var user = await _accounts.GetUserByEmail("laimis@gmail.com");
            if (user == null)
            {
                _logger.LogError("Could not find user for stop monitoring");
                return;
            }

            var stocks = await _portfolio.GetStocks(user.Id);
            var positions = stocks
                .Where(s => s.State.OpenPosition != null)
                .Select(s => s.State.OpenPosition)
                .Where(p => p.StopPrice != null);

            foreach (var position in positions)
            {
                var priceResponse = await _brokerage.GetQuote(user.State, position.Ticker);
                if (!priceResponse.IsOk)
                {
                    _logger.LogError($"Could not get price for {position.Ticker}: {priceResponse.Error.Message}");
                    continue;
                }

                var price = priceResponse.Success.lastPrice;

                if (price <= position.StopPrice.Value)
                {
                    StopPriceMonitor.Register(
                        container: _container,
                        price: price,
                        stopPrice: position.StopPrice.Value,
                        ticker: position.Ticker,
                        when: DateTimeOffset.UtcNow,
                        userId: user.Id
                    );
                }
                else
                {
                    StopPriceMonitor.Deregister(_container, position.Ticker, user.Id);
                }
            }
        }

        private DateTimeOffset GetNextStopLossCheckTime()
        {
            if (_marketHours.IsMarketOpen(DateTimeOffset.UtcNow))
            {
                return DateTimeOffset.UtcNow.AddMinutes(5);
            }
            
            var marketTimeNow = _marketHours.ToMarketTime(DateTimeOffset.UtcNow);

            if (marketTimeNow.DayOfWeek == DayOfWeek.Saturday)
            {
                return _marketHours.GetMarketStartOfDayTimeInUtc(marketTimeNow.Date.AddDays(2)).AddMinutes(10);
            }

            if (marketTimeNow.DayOfWeek == DayOfWeek.Sunday)
            {
                return _marketHours.GetMarketStartOfDayTimeInUtc(marketTimeNow.Date.AddDays(1)).AddMinutes(10);
            }

            var openTime = _marketHours.GetMarketStartOfDayTimeInUtc(marketTimeNow.Date);
            var closeTime = _marketHours.GetMarketEndOfDayTimeInUtc(marketTimeNow.Date);

            return marketTimeNow.TimeOfDay switch {
                var t when t <= openTime.TimeOfDay => 
                    _marketHours.GetMarketStartOfDayTimeInUtc(marketTimeNow.Date).AddMinutes(10),
                var t when t >= closeTime.TimeOfDay => 
                    _marketHours.GetMarketStartOfDayTimeInUtc(marketTimeNow.Date.AddDays(1)).AddMinutes(10),
                _ => throw new Exception("Should not be possible to get here but did with time: " + marketTimeNow)
            };
        }

        private DateTimeOffset GetNextMonitorRunTime()
        {
            DateTimeOffset LogAndReturn(string message, DateTimeOffset delay)
            {
                _logger.LogInformation(message);
                return delay;
            }
            
            var currentTimeInEastern = _marketHours.ToMarketTime(DateTimeOffset.UtcNow);
            var currentCloseTime = _marketHours.GetMarketEndOfDayTimeInUtc(currentTimeInEastern);
            var currentOpenTime = _marketHours.GetMarketStartOfDayTimeInUtc(currentTimeInEastern);
            var lastRunBeforeClose = currentCloseTime.AddMinutes(-30);

            var nextRunTime = 
                currentTimeInEastern.TimeOfDay switch {
                var t when t >= lastRunBeforeClose.TimeOfDay => 
                    LogAndReturn(
                        "After the end of the market day",
                        _marketHours.GetMarketStartOfDayTimeInUtc(currentTimeInEastern.AddDays(1))
                            .AddMinutes(15)
                    ),
                var t when t < currentOpenTime.TimeOfDay => 
                    LogAndReturn(
                        "Before the start of the market day",
                        _marketHours.GetMarketStartOfDayTimeInUtc(currentTimeInEastern)
                            .AddMinutes(15)
                    ),
                var t when t < lastRunBeforeClose.TimeOfDay =>
                    LogAndReturn(
                        "In the middle of the day",
                        lastRunBeforeClose
                    ),
                    
                _ =>  throw new Exception("Should not be possible to get here but did with time: " + currentTimeInEastern)
            };

            return nextRunTime;
        }

        private async Task<List<AlertCheck>> MonitorForGaps(List<AlertCheck> checks, CancellationToken ct)
        {
            var completed = new List<AlertCheck>();

            foreach (var c in checks)
            {
                if (ct.IsCancellationRequested)
                {
                    return completed;
                }

                var prices = await GetPricesForTicker(c.user, c.ticker);

                if (!prices.IsOk)
                {
                    _logger.LogCritical($"Failed to get price history for {c.ticker}: {prices.Error.Message}");
                    continue;
                }

                completed.Add(c);

                var gaps = GapAnalysis.Generate(prices.Success, 2);
                if (gaps.Count == 0 || gaps[0].type != GapType.Up)
                {
                    GapUpMonitor.Deregister(_container, c.ticker, c.user.Id);
                    continue;
                }

                var gap = gaps[0];

                GapUpMonitor.Register(
                    container: _container,
                    ticker: c.ticker, gap: gap, when: DateTimeOffset.UtcNow, userId: c.user.Id
                );
            }
        
            return completed;
        }

        private async Task<List<AlertCheck>> MonitorForUpsideReversals(List<AlertCheck> checks, CancellationToken ct)
        {
            var completed = new List<AlertCheck>();

            foreach (var c in checks)
            {
                if (ct.IsCancellationRequested)
                {
                    return completed;
                }

                var prices = await GetPricesForTicker(user: c.user, ticker: c.ticker);
                if (!prices.IsOk)
                {
                    _logger.LogCritical($"Failed to get price history for {c.ticker}: {prices.Error.Message}");
                    continue;
                }

                completed.Add(c);

                var patterns = PatternDetection.Generate(prices.Success).ToList();
                if (patterns.Count == 0)
                {
                    foreach(var patternName in PatternDetection.AvailablePatterns)
                    {
                        PatternAlert.Deregister(
                            container: _container,
                            ticker: c.ticker,
                            patternName: patternName,
                            userId: c.user.Id
                        );
                    }
                    continue;
                }

                foreach(var pattern in patterns)
                {
                    PatternAlert.Register(
                        container: _container,
                        ticker: c.ticker,
                        pattern: pattern,
                        price: prices.Success.Last().Close,
                        when: DateTimeOffset.UtcNow,
                        userId: c.user.Id
                    );
                }
            }
        
            return completed;
        }

        private async Task<ServiceResponse<core.Shared.Adapters.Stocks.PriceBar[]>> GetPricesForTicker(UserState user, string ticker)
        {
            var start = _marketHours.GetMarketStartOfDayTimeInUtc(DateTime.UtcNow.AddDays(-7));
            var end = _marketHours.GetMarketEndOfDayTimeInUtc(DateTime.UtcNow);
            var prices = await _brokerage.GetPriceHistory(
                state: user,
                ticker: ticker,
                frequency: core.Shared.Adapters.Stocks.PriceFrequency.Daily,
                start: start,
                end: end
            );
            return prices;
        }
    }
}