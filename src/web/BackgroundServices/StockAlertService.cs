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
        private DateTimeOffset _nextStopLossCheck = DateTimeOffset.MinValue;
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _container.AddNotice("Stock alert service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {    
                try
                {
                    var user = new Lazy<Task<UserState>>(GetUser);

                    await RunThroughListMonitoringChecks(user, stoppingToken);

                    await RunThroughStopLossChecks(user, stoppingToken);

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

        private async Task RunThroughStopLossChecks(Lazy<Task<UserState>> userFunc, CancellationToken cancellationToken)
        {
            if (DateTimeOffset.UtcNow > _nextStopLossCheck)
            {
                _container.ToggleStopLossCheckCompleted(false);

                var user = await userFunc.Value;
                if (user == null)
                {
                    return;
                }

                var checks = await AlertCheckGenerator.GetStopLossChecks(_portfolio, user);

                var completed = await core.Alerts.Services.Monitors.MonitorForStopLosses(
                    quoteFunc: GetQuote,
                    container: _container,
                    checks: checks,
                    cancellationToken: cancellationToken
                );

                // only update the next run time if we've completed all checks
                if (completed.Count == checks.Count)
                {
                    _nextStopLossCheck = ScanScheduling.GetNextStopLossMonitorRunTime(
                        DateTimeOffset.UtcNow,
                        _marketHours
                    );

                    _container.ToggleStopLossCheckCompleted(true);
                }

                _container.AddNotice($"Completed {completed.Count} out of {checks.Count} stop loss checks, next run at {_marketHours.ToMarketTime(_nextStopLossCheck)}");
            }
        }

        private async Task RunThroughListMonitoringChecks(Lazy<Task<UserState>> user, CancellationToken stoppingToken)
        {
            if (DateTimeOffset.UtcNow > _nextListMonitoringRun || _container.ManualRunRequested())
            {
                _container.ToggleListCheckCompleted(false);

                await GenerateListMonitoringChecks(user);
            }

            var pricesService = new GetPricesForTickerService(
                _brokerage,
                _marketHours,
                _logger,
                user
            );

            foreach (var kp in _listChecks.Where(kp => kp.Value.Count > 0))
            {
                var scanner = core.Alerts.Services.Monitors.GetScannerForTag(
                    tag: kp.Key,
                    pricesFunc: pricesService.GetPricesForTicker,
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

        private async Task GenerateListMonitoringChecks(Lazy<Task<UserState>> userFunc)
        {
            var user = await userFunc.Value;
            if (userFunc == null)
            {
                return;
            }

            _listChecks.Clear();

            foreach (var m in core.Alerts.Services.Monitors.GetMonitors())
            {
                var list = await AlertCheckGenerator.GetStocksFromListsWithTags(
                    _portfolio, m.tag, user
                );

                _listChecks.Add(m.tag, list);
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

        private async Task<UserState> GetUser()
        {
            var user = await _accounts.GetUserByEmail("laimis@gmail.com");
            if (user == null)
            {
                _logger.LogCritical("No user found for stock alert service");
                _container.AddNotice("No user found for stock alert service");            
            }
            return user?.State;
        }

        private class GetPricesForTickerService
        {
            private readonly IBrokerage _brokerage;
            private readonly IMarketHours _marketHours;
            private readonly ILogger _logger;
            private readonly Lazy<Task<UserState>> _user;
            private readonly Dictionary<string, ServiceResponse<PriceBar[]>> _cache = new();

            public GetPricesForTickerService(
                IBrokerage brokerage,
                IMarketHours marketHours,
                ILogger logger,
                Lazy<Task<UserState>> user)
            {
                _brokerage = brokerage;
                _marketHours = marketHours;
                _logger = logger;
                _user = user;
            }

            public async Task<ServiceResponse<PriceBar[]>> GetPricesForTicker(string ticker)
            {
                if (_cache.TryGetValue(ticker, out ServiceResponse<PriceBar[]> value))
                {
                    return value;
                }

                var start = _marketHours.GetMarketStartOfDayTimeInUtc(DateTime.UtcNow.AddDays(-365));
                var end = _marketHours.GetMarketEndOfDayTimeInUtc(DateTime.UtcNow);
                var user = await _user.Value;
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
                    _cache.Add(ticker, prices);
                }

                return prices;
            }
        }
        

        private async Task<ServiceResponse<StockQuote>> GetQuote(
            UserState user, string ticker)
        {
            var priceResponse = await _brokerage.GetQuote(user, ticker);
            if (!priceResponse.IsOk)
            {
                _logger.LogError("Could not get price for {ticker}: {message}", ticker, priceResponse.Error.Message);
            }
            return priceResponse;
        }
    }
}