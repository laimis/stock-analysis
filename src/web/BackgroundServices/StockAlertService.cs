using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.Account;
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
        private ILogger<StockAlertService> _logger;
        private IMarketHours _marketHours;
        private IPortfolioStorage _portfolio;

        public StockAlertService(
            IAccountStorage accounts,
            IBrokerage brokerage,
            StockAlertContainer container,
            ILogger<StockAlertService> logger,
            IMarketHours marketHours,
            IPortfolioStorage portfolio)
        {
            _accounts = accounts;
            _brokerage = brokerage;
            _container = container;
            _logger = logger;
            _marketHours = marketHours;
            _portfolio = portfolio;
        }

        private const string GAP_UP_TAG = "monitor:gapup";
        private const string UPSIDE_REVERSAL_TAG = "monitor:upsidereversal";
        private DateTimeOffset _nextAlertUpdateRun = DateTimeOffset.MinValue;
        private DateTimeOffset _nextStopLossCheck = DateTimeOffset.MinValue;
        private List<AlertCheck> _gapUpChecks;
        private List<AlertCheck> _upsideReversalChecks;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {    
                try
                {
                    if (DateTimeOffset.UtcNow > _nextAlertUpdateRun || _container.ManualRunRequested())
                    {
                        var user = await _accounts.GetUserByEmail("laimis@gmail.com");
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
                            $"Alert check generator added {_gapUpChecks.Count + _upsideReversalChecks.Count} checks, next run at {_nextAlertUpdateRun}"
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
                        
                        _container.AddNotice($"{upsideReversalsCompleted.Count} gap up checks completed, {_upsideReversalChecks.Count} remaining");
                    }

                    if (DateTimeOffset.UtcNow > _nextStopLossCheck)
                    {
                        await MonitorForStops(stoppingToken);
                        _nextStopLossCheck = GetNextStopLossCheckTime();
                        _container.AddNotice("Stop loss monitor complete, next run at " + _nextStopLossCheck);
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
                    _container.Register(
                        StopPriceMonitor.Create(
                            price: price,
                            stopPrice: position.StopPrice.Value,
                            ticker: position.Ticker,
                            when: DateTimeOffset.UtcNow,
                            userId: user.Id
                        )
                    );
                }
                else
                {
                    _container.Deregister(
                        StopPriceMonitor.Description, position.Ticker, user.Id);
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

            return marketTimeNow.TimeOfDay switch {
                var t when t <= web.Utils.MarketHours.StartTime => 
                    _marketHours.GetMarketStartOfDayTimeInUtc(marketTimeNow.Date).AddMinutes(10),
                var t when t >= web.Utils.MarketHours.CloseToEndTime => 
                    _marketHours.GetMarketStartOfDayTimeInUtc(marketTimeNow.Date.AddDays(1)).AddMinutes(10),
                _ => throw new Exception("Should not get here for scheduling stop loss check")
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
            var nextRunTime = 
                currentTimeInEastern.TimeOfDay switch {
                var t when t >= web.Utils.MarketHours.CloseToEndTime => 
                    LogAndReturn(
                        "After the end of the market day",
                        _marketHours.GetMarketEndOfDayTimeInUtc(currentTimeInEastern.Date.AddDays(1))
                            .AddMinutes(-30)
                    ),
                var t when t < web.Utils.MarketHours.StartTime => 
                    LogAndReturn(
                        "Before the start of the market day",
                        _marketHours.GetMarketEndOfDayTimeInUtc(currentTimeInEastern.Date)
                            .AddMinutes(-30)
                    ),
                var t when t < web.Utils.MarketHours.CloseToEndTime =>
                    LogAndReturn(
                        "In the middle of the day",
                        _marketHours.GetMarketStartOfDayTimeInUtc(currentTimeInEastern.Date)
                        .Add(web.Utils.MarketHours.CloseToEndTime)
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
                    _container.Deregister(GapUpMonitor.GapUp, c.ticker, c.user.Id);
                    continue;
                }

                var gap = gaps[0];

                _container.Register(
                    GapUpMonitor.Create(ticker: c.ticker, gap: gap, when: DateTimeOffset.UtcNow, userId: c.user.Id)
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
                    _container.Deregister(UpsideReversalAlert.Description, c.ticker, c.user.Id);
                    continue;
                }

                _container.Register(
                    UpsideReversalAlert.Create(
                        price: prices.Success.Last().Close,
                        when: DateTimeOffset.UtcNow,
                        ticker: c.ticker,
                        userId: c.user.Id
                ));
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