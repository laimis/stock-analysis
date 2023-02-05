using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Alerts;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Stocks.Services.Analysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class ListMonitorService : BackgroundService
    {
        private IAccountStorage _accounts;
        private IBrokerage _brokerage;
        private StockMonitorContainer _container;
        private ILogger<ListMonitorService> _logger;
        private IMarketHours _marketHours;
        private IPortfolioStorage _portfolio;

        public ListMonitorService(
            IAccountStorage accounts,
            IBrokerage brokerage,
            StockMonitorContainer container,
            ILogger<ListMonitorService> logger,
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
        private DateTimeOffset _nextGapUpRun = DateTimeOffset.MinValue;
        
        private const string UPSIDE_REVERSAL_TAG = "monitor:upsidereversal";
        private DateTimeOffset _nextUpsideReversalRun = DateTimeOffset.MinValue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {    
                try
                {
                    if (DateTimeOffset.UtcNow > _nextGapUpRun)
                    {
                        await MonitorForGaps(stoppingToken);
                    }

                    if (DateTimeOffset.UtcNow > _nextUpsideReversalRun)
                    {
                        await MonitorForUpsideReversals(stoppingToken);
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed while running gap monitor, will sleep");
                    await Task.Delay(TimeSpan.FromMinutes(10));
                }
            }
        }

        private DateTimeOffset GetNextUpsideReveralMonitorRunTime()
        {
            DateTimeOffset LogAndReturn(string message, DateTimeOffset delay)
            {
                _logger.LogInformation(message);
                return delay;
            }
            
            // we want it to run an hour before close

            var currentTimeInEastern = _marketHours.ToMarketTime(DateTimeOffset.UtcNow);
            var nextRunTime = 
                currentTimeInEastern.TimeOfDay switch {
                var t when t >= web.Utils.MarketHours.CloseToEndTime => 
                    LogAndReturn(
                        "After the end of the market day",
                        _marketHours.GetMarketEndOfDayTimeInUtc(currentTimeInEastern.Date.AddDays(1))
                            .AddHours(-1)
                    ),
                var t when t < web.Utils.MarketHours.StartTime => 
                    LogAndReturn(
                        "Before the start of the market day",
                        _marketHours.GetMarketEndOfDayTimeInUtc(currentTimeInEastern.Date)
                            .AddHours(-1)
                    ),
                var t when t < web.Utils.MarketHours.CloseToEndTime =>
                    LogAndReturn(
                        "In the middle of the day",
                        _marketHours.GetMarketEndOfDayTimeInUtc(currentTimeInEastern.Date)
                        .AddHours(-1)
                    ),
                    
                _ =>  throw new Exception("Should not be possible to get here but did with time: " + currentTimeInEastern)
            };

            return nextRunTime;
        }

        private DateTimeOffset GetNextGapUpMonitorRunTime()
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
                        _marketHours.GetMarketStartOfDayTimeInUtc(currentTimeInEastern.Date.AddDays(1))
                            .AddMinutes(5)
                    ),
                var t when t < web.Utils.MarketHours.StartTime => 
                    LogAndReturn(
                        "Before the start of the market day",
                        _marketHours.GetMarketStartOfDayTimeInUtc(currentTimeInEastern.Date)
                            .AddMinutes(5)
                    ),
                var t when t < web.Utils.MarketHours.CloseToEndTime =>
                    LogAndReturn(
                        "In the middle of the day",
                        _marketHours.GetMarketEndOfDayTimeInUtc(currentTimeInEastern.Date)
                        .AddMinutes(-15)
                    ),
                    
                _ =>  throw new Exception("Should not be possible to get here but did with time: " + currentTimeInEastern)
            };

            return nextRunTime;
        }

        private async Task MonitorForGaps(CancellationToken ct)
        {
            var (user, tickers) = await GetStocksFromListsWithTags(GAP_UP_TAG);
            if (user == null)
            {
                _logger.LogCritical("User not found for gap monitoring");
                return;
            }

            foreach (var ticker in tickers)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                var prices = await GetPricesForTicker(user, ticker);

                if (!prices.IsOk)
                {
                    _logger.LogCritical($"Failed to get price history for {ticker}: {prices.Error.Message}");
                    continue;
                }

                var gaps = GapAnalysis.Generate(prices.Success, 2);
                if (gaps.Count == 0 || gaps[0].type != GapType.Up)
                {
                    _container.Deregister(GapUpMonitor.GapUp, ticker, user.Id);
                    continue;
                }

                var gap = gaps[0];

                _container.Register(
                    new GapUpMonitor(ticker: ticker, gap: gap, when: DateTimeOffset.UtcNow, userId: user.Id)
                );
            }
        
            _logger.LogInformation($"Finished gap up monitoring for {tickers.Length} tickers");

            _nextGapUpRun = GetNextGapUpMonitorRunTime();
        }

        private async Task MonitorForUpsideReversals(CancellationToken ct)
        {
            var (user, tickers) = await GetStocksFromListsWithTags(UPSIDE_REVERSAL_TAG);
            if (user == null)
            {
                _logger.LogCritical($"User not found for {UPSIDE_REVERSAL_TAG} monitoring");
                return;
            }

            foreach (var ticker in tickers)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                var prices = await GetPricesForTicker(user, ticker);

                if (!prices.IsOk)
                {
                    _logger.LogCritical($"Failed to get price history for {ticker}: {prices.Error.Message}");
                    continue;
                }

                var patterns = PatternDetection.Generate(prices.Success).ToList();
                if (patterns.Count == 0)
                {
                    _container.Deregister(PatternDetection.UpsideReversal, ticker, user.Id);
                    continue;
                }

                _container.Register(
                    new AlwaysOnMonitor(
                        description: $"Upside reversal for {ticker}",
                        source: PatternDetection.UpsideReversal,
                        ticker: ticker,
                        userId: user.Id,
                        value: 0
                ));
            }
        
            _logger.LogInformation($"Finished {UPSIDE_REVERSAL_TAG} monitoring for {tickers.Length} tickers");

            _nextUpsideReversalRun = GetNextUpsideReveralMonitorRunTime();
        }

        private async Task<ServiceResponse<core.Shared.Adapters.Stocks.PriceBar[]>> GetPricesForTicker(User user, string ticker)
        {
            var start = _marketHours.GetMarketStartOfDayTimeInUtc(DateTime.UtcNow.AddDays(-7));
            var end = _marketHours.GetMarketEndOfDayTimeInUtc(DateTime.UtcNow);
            var prices = await _brokerage.GetPriceHistory(
                state: user.State,
                ticker: ticker,
                frequency: core.Shared.Adapters.Stocks.PriceFrequency.Daily,
                start: start,
                end: end
            );
            return prices;
        }

        private async Task<(User, string[])> GetStocksFromListsWithTags(string tag)
        {
            var user = await _accounts.GetUserByEmail("laimis@gmail.com");
    
            var list = await _portfolio.GetStockLists(user.Id);

            var tickerList = list
                .Where(l => l.State.ContainsTag(tag))
                .SelectMany(x => x.State.Tickers).Select(t => t.Ticker).ToArray();

            return (user, tickerList);
        }
    }
}