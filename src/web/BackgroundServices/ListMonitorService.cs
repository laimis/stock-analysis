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
                    _logger.LogCritical(ex, "Failed to run gap up monitor");
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
            TimeSpan ClosedMarketLogic(DateTimeOffset utcReferenceTime)
            {
                var currentTime = _marketHours.ToMarketTime(utcReferenceTime);
                var marketCloseTime = _marketHours.GetMarketEndOfDayTimeInUtc(currentTime);

                var openTime = (currentTime > marketCloseTime) switch {
                    true => _marketHours.GetMarketStartOfDayTimeInUtc(currentTime.AddDays(-1)),
                    false => _marketHours.GetMarketStartOfDayTimeInUtc(currentTime)
                };

                return openTime - currentTime;
            }

            TimeSpan OpenMarketLogic(DateTimeOffset utcReferenceTime)
            {
                var endOfMarket = _marketHours.GetMarketEndOfDayTimeInUtc(utcReferenceTime);
                var closeTime = endOfMarket.AddMinutes(-15);
                return closeTime - utcReferenceTime;
            }

            var nowUtc = DateTimeOffset.UtcNow;
            var delay = _marketHours.IsMarketOpen(nowUtc) switch {
                true => OpenMarketLogic(nowUtc),
                false => ClosedMarketLogic(nowUtc)
            };

            return delay;
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
                
                var description = $"Gap up for {ticker}: {Math.Round(gap.gapSizePct * 100, 2)}%";
                _container.Register(new GapUpMonitor(ticker: ticker, price: gap.bar.Close, when: DateTimeOffset.UtcNow, userId: user.Id, description: description));
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