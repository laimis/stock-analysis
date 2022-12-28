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
                await MonitorForGaps(stocks, user, stoppingToken);

                var nextScanTime = _marketHours.IsMarketOpen(DateTimeOffset.UtcNow) switch {
                    true => _marketHours.GetMarketStartOfDayTimeInUtc(DateTimeOffset.UtcNow.AddDays(1)),
                    false => _marketHours.GetMarketStartOfDayTimeInUtc(DateTimeOffset.UtcNow)
                };

                var delay = nextScanTime - DateTimeOffset.UtcNow;

                _logger.LogInformation("Next scan in {delay}", delay);

                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task MonitorForGaps(string[] tickers, User user, CancellationToken ct)
        {
            var start = _marketHours.GetMarketStartOfDayTimeInUtc(DateTime.UtcNow.AddDays(-1));
            var end = _marketHours.GetMarketEndOfDayTimeInUtc(DateTime.UtcNow);

            try
            {
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

                    var gaps = GapAnalysis.Generate(prices.Success, 2);
                    if (gaps.Count == 0)
                    {
                        _logger.LogInformation($"No gaps found for {ticker}");
                        continue;
                    }

                    var gap = gaps[0];
                    if (gap.type != GapType.Up)
                    {
                        _logger.LogInformation($"Gap down for {ticker}: {gap}");
                        continue;
                    }

                    var description = $"Gap up for {ticker}: {Math.Round(gap.gapSizePct * 100, 2)}%";
                    _container.Register(new GapUpMonitor(ticker: ticker, price: gap.bar.Close, when: DateTimeOffset.UtcNow, userId: user.Id, description: description));
                    _logger.LogInformation(description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to run gap up monitor");
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