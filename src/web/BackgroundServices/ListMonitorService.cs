using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Alerts;
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var (user, stocks) = await GetStocksOfInterest();
            if (user == null)
            {
                _logger.LogCritical("User not found for gap monitoring");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {    
                try
                {
                    await MonitorForGaps(stocks, user, stoppingToken);

                    await DelayUntilNextScan(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed while running gap monitor, will sleep");
                    await Task.Delay(TimeSpan.FromMinutes(10));
                }
            }
        }

        private static readonly TimeSpan _minimumDelay = TimeSpan.FromMinutes(10);
        private async Task DelayUntilNextScan(CancellationToken stoppingToken)
        {
            var delay = GetDelayTime();
            if (delay < _minimumDelay)
            {
                _logger.LogInformation($"Delay time {delay} is less than minimum delay {_minimumDelay}, using minimum delay instead");
                delay = _minimumDelay;
            }

            _logger.LogInformation("Next scan in {delay}", delay);

            await Task.Delay(delay, stoppingToken);
        }

        private TimeSpan GetDelayTime()
        {
            // I want to calculate the next market open time, and then delay until that time

            var currentTimeInEastern = _marketHours.ToMarketTime(DateTime.UtcNow);

            TimeSpan LogAndReturn(string message, TimeSpan delay)
            {
                _logger.LogInformation(message);
                return delay;
            }

            return currentTimeInEastern.TimeOfDay switch {
                var t when t >= web.Utils.MarketHours.CloseToEndTime => 
                    LogAndReturn(
                        "After the end of the market day",
                        _marketHours.GetMarketStartOfDayTimeInUtc(currentTimeInEastern.Date.AddDays(1))
                            .AddMinutes(5)
                            .Subtract(DateTimeOffset.UtcNow)
                    ),
                var t when t < web.Utils.MarketHours.StartTime => 
                    LogAndReturn(
                        "Before the start of the market day",
                        _marketHours.GetMarketStartOfDayTimeInUtc(currentTimeInEastern.Date)
                            .AddMinutes(5)
                            .Subtract(DateTimeOffset.UtcNow)
                    ),
                var t when t < web.Utils.MarketHours.CloseToEndTime =>
                    LogAndReturn(
                        "In the middle of the day",
                        _marketHours.GetMarketEndOfDayTimeInUtc(currentTimeInEastern.Date)
                        .AddMinutes(-15)
                        .Subtract(DateTimeOffset.UtcNow)
                    ),
                    
                _ =>  throw new Exception("Should not be possible to get here but did with time: " + currentTimeInEastern)
            };
        }

        private async Task MonitorForGaps(string[] tickers, User user, CancellationToken ct)
        {
            var start = _marketHours.GetMarketStartOfDayTimeInUtc(DateTime.UtcNow.AddDays(-7));
            var end = _marketHours.GetMarketEndOfDayTimeInUtc(DateTime.UtcNow);
            
            foreach (var ticker in tickers)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                var prices = await _brokerage.GetPriceHistory(
                    state: user.State,
                    ticker: ticker,
                    frequency: core.Shared.Adapters.Stocks.PriceFrequency.Daily,
                    start: start,
                    end: end
                );

                if (!prices.IsOk)
                {
                    _logger.LogCritical($"Failed to get price history for {ticker}: {prices.Error.Message}");
                    continue;
                }

                _logger.LogInformation($"Found {prices.Success.Length} bars for {ticker} between {start} and {end}");

                var gaps = GapAnalysis.Generate(prices.Success, 2);
                if (gaps.Count == 0 || gaps[0].type != GapType.Up)
                {
                    _logger.LogInformation($"No gaps found for {ticker}");
                    _container.Deregister(GapUpMonitor.MonitorIdentifer, ticker, user.Id);
                    continue;
                }

                var gap = gaps[0];
                
                _container.Register(new GapUpMonitor(ticker: ticker, gap: gap, when: DateTimeOffset.UtcNow, userId: user.Id));
            }
        }

        private async Task<(User, string[])> GetStocksOfInterest()
        {
            var user = await _accounts.GetUserByEmail("laimis@gmail.com");
    
            var list = await _portfolio.GetStockLists(user.Id);

            var tickerList = list.SelectMany(x => x.State.Tickers).Select(t => t.Ticker).ToArray();

            return (user, tickerList);
        }
    }
}