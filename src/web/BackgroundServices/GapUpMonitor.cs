using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared.Adapters.Brokerage;
using core.Stocks.Services.Analysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class GapUpMonitor : BackgroundService
    {
        private IAccountStorage _accounts;
        private IBrokerage _brokerage;
        private ILogger<GapUpMonitor> _logger;
        private IMarketHours _marketHours;

        public GapUpMonitor(
            IAccountStorage accounts,
            IBrokerage brokerage,
            ILogger<GapUpMonitor> logger,
            IMarketHours marketHours)
        {
            _accounts = accounts;
            _brokerage = brokerage;
            _logger = logger;
            _marketHours = marketHours;
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

        private async Task MonitorForGaps(string[] stocks, User user, CancellationToken ct)
        {
            var start = DateTime.UtcNow.AddDays(-1);
            var daysToCheck = 4;
            while (daysToCheck > 0)
            {
                var marketHours = await _brokerage.GetMarketHours(user.State, start);
                if (marketHours.IsOk && marketHours.Success.isOpen)
                {
                    break;
                }
                start = start.AddDays(-1);
                daysToCheck--;
            }
            
            var end = DateTimeOffset.UtcNow;
            var endHoursCheck = await _brokerage.GetMarketHours(user.State, end);
            if (endHoursCheck.IsOk && !endHoursCheck.Success.isOpen)
            {
                _logger.LogCritical($"Market is closed for {end}");
                return;
            }

            try
            {
                foreach (var stock in stocks)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    var prices = await _brokerage.GetPriceHistory(
                        state: user.State,
                        ticker: stock,
                        frequency: core.Shared.Adapters.Stocks.PriceFrequency.Daily,
                        start: start,
                        end: end
                    );

                    if (!prices.IsOk)
                    {
                        _logger.LogCritical($"Failed to get price history for {stock}: {prices.Error.Message}");
                        continue;
                    }

                    var gaps = GapAnalysis.Generate(prices.Success, 2);
                    if (gaps.Count == 0)
                    {
                        // TODO: change to information
                        _logger.LogCritical($"No gaps found for {stock}");
                        continue;
                    }

                    var gap = gaps[0];
                    if (gap.type != GapType.Up)
                    {
                        // TODO: change to information
                        _logger.LogCritical($"Gap down for {stock}: {gap}");
                        continue;
                    }

                    var description = $"Gap up for {stock}: {Math.Round(gap.gapSizePct * 100, 2)}%";
                    // TODO: change to information
                    _logger.LogCritical(description);
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
    
            return (user, new string[] { "AAPL", "MSFT" });
        }
    }
}