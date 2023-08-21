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
using core.Shared.Adapters.Stocks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class StockAlertService : BackgroundService
    {
        private readonly IAccountStorage _accounts;
        private readonly IBrokerage _brokerage;
        private readonly StockAlertContainer _container;
        private readonly ILogger<StockAlertService> _logger;
        private readonly IMarketHours _marketHours;
        private readonly IPortfolioStorage _portfolio;
        private readonly Dictionary<string, ServiceResponse<PriceBar[]>> _priceCache = new();
        
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

        private static readonly TimeSpan _sleepDuration = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan _sleepDurationFailureMode = TimeSpan.FromMinutes(5);

        private readonly Dictionary<string, List<AlertCheck>> _listChecks = new();
        private DateTimeOffset _nextListMonitoringRun = DateTimeOffset.MinValue;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _container.AddNotice("Stock alert service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {    
                try
                {
                    await RunThroughListMonitoringChecks(stoppingToken);

                    await Task.Delay(_sleepDuration, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed while running alert monitor, will sleep");
                    _container.AddNotice("Failed while running alert monitor: " + ex.Message);
                    _container.ManualRunRequested();
                    await Task.Delay(_sleepDurationFailureMode, stoppingToken);
                }
            }
        }

        private async Task RunThroughListMonitoringChecks(CancellationToken stoppingToken)
        {
            if (DateTimeOffset.UtcNow > _nextListMonitoringRun || _container.ManualRunRequested())
            {
                _container.ToggleListCheckCompleted(false);

                await GenerateListMonitoringChecks();
            }

            _priceCache.Clear();

            foreach (var kp in _listChecks.Where(kp => kp.Value.Count > 0))
            {
                var scanner = core.Alerts.Services.Monitors.GetScannerForTag(
                    tag: kp.Key,
                    pricesFunc: GetPricesForTicker,
                    container: _container,
                    checks: kp.Value,
                    cancellationToken: stoppingToken
                );

                var completed = await scanner();

                foreach (var c in completed)
                {
                    kp.Value.Remove(c);
                }

                _container.AddNotice($"{completed.Count} {kp.Key} checks completed, {kp.Value.Count} remaining");
            }

            if (_listChecks.All(kp => kp.Value.Count == 0))
            {
                _container.ToggleListCheckCompleted(true);
            }
        }

        private async Task<ServiceResponse<PriceBar[]>> GetPricesForTicker(UserState user, string ticker)
        {
            if (_priceCache.TryGetValue(ticker, out ServiceResponse<PriceBar[]> value))
            {
                return value;
            }

            var start = _marketHours.GetMarketStartOfDayTimeInUtc(DateTime.UtcNow.AddDays(-365));
            var end = _marketHours.GetMarketEndOfDayTimeInUtc(DateTime.UtcNow);
            var prices = await _brokerage.GetPriceHistory(
                state: user,
                ticker: ticker,
                frequency: PriceFrequency.Daily,
                start: start,
                end: end
            );

            if (!prices.IsOk)
            {
                _logger.LogCritical("Could not get price history for {ticker}: {message}", ticker, prices.Error.Message);
            }
            else
            {
                _priceCache.Add(ticker, prices);
            }

            return prices;
        }

        private async Task GenerateListMonitoringChecks()
        {
            _listChecks.Clear();
            
            foreach (var m in core.Alerts.Services.Monitors.GetMonitors())
            {
                var users = await _accounts.GetUserEmailIdPairs();

                foreach(var (_, userId) in users)    
                {
                    var user = await _accounts.GetUser(new Guid(userId));
                    var list = (await _portfolio.GetStockLists(user.Id))
                        .Where(l => l.State.ContainsTag(m.tag))
                        .SelectMany(l => l.State.Tickers.Select(t => (l, t)))
                        .Select(listTickerPair => new AlertCheck(ticker: listTickerPair.t.Ticker, listName: listTickerPair.l.State.Name, user: user.State))
                        .ToList();

                    _listChecks.Add(m.tag, list);
                }
            }

            _nextListMonitoringRun = ScanScheduling.GetNextListMonitorRunTime(
                DateTimeOffset.UtcNow,
                _marketHours
            );

            _container.ManualRunCompleted();

            var description = string.Join(", ", _listChecks.Select(kp => $"{kp.Key} {kp.Value.Count} checks"));

            _container.AddNotice(
                $"Alert check generator added {description}, next run at {_marketHours.ToMarketTime(_nextListMonitoringRun)}"
            );
        }
    }
}