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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    // TODO: this class is doing too much, need to rethink
    // how to structure the logic contained here.
    // 1. it's in charge of scheduling the runs
    // 2. it's in charge of pulling which tickers to scan, and how those scans should work
    // 3. it's in charge of notification logic
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

        private Dictionary<string, List<AlertCheck>> _listChecks = new Dictionary<string, List<AlertCheck>>();
        private DateTimeOffset _nextListMonitoringRun = DateTimeOffset.MinValue;
        private DateTimeOffset _nextStopLossCheck = DateTimeOffset.MinValue;
        private DateTimeOffset _nextEmailSend = DateTimeOffset.MinValue;
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {    
                try
                {
                    await RunThroughListMonitoringChecks(stoppingToken);

                    await RunThroughStopLossChecks(stoppingToken);

                    await SendAlertSummaryEmail();

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

        private async Task RunThroughStopLossChecks(CancellationToken cancellationToken)
        {
            if (DateTimeOffset.UtcNow > _nextStopLossCheck)
            {
                var user = await GetUser();
                if (user == null)
                {
                    return;
                }

                var checks = await AlertCheckGenerator.GetStopLossChecks(_portfolio, user.State);

                var completed = await Scanners.MonitorForStopLosses(
                    quoteFunc: (u, ticker) => GetQuote(u, ticker),
                    container: _container,
                    checks: checks,
                    cancellationToken: cancellationToken
                );

                _nextStopLossCheck = ScanScheduling.GetNextStopLossMonitorRunTime(
                    DateTimeOffset.UtcNow,
                    _marketHours
                );
                _container.AddNotice($"Completed {completed.Count} stop loss checks, next run at {_marketHours.ToMarketTime(_nextStopLossCheck)}");
            }
        }

        private async Task RunThroughListMonitoringChecks(CancellationToken stoppingToken)
        {
            if (DateTimeOffset.UtcNow > _nextListMonitoringRun || _container.ManualRunRequested())
            {
                await GenerateListMonitoringChecks();
            }

            foreach (var kp in _listChecks.Where(kp => kp.Value.Count > 0))
            {
                var scanner = Scanners.GetScannerForTag(
                    tag: kp.Key,
                    pricesFunc: (user, ticker) => GetPricesForTicker(user, ticker),
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
        }

        private async Task GenerateListMonitoringChecks()
        {
            var user = await GetUser();
            if (user == null)
            {
                return;
            }

            _listChecks.Clear();

            foreach (var tag in Scanners.GetTags())
            {
                var list = await AlertCheckGenerator.GetStocksFromListsWithTags(
                    _portfolio, tag, user.State
                );

                _listChecks.Add(tag, list);
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

        private async Task<User> GetUser()
        {
            var user = await _accounts.GetUserByEmail("laimis@gmail.com");
            if (user == null)
            {
                _logger.LogCritical("No user found for stock alert service");
                _container.AddNotice("No user found for stock alert service");            
            }
            return user;
        }

        private async Task SendAlertSummaryEmail()
        {
            // wait to send emails if there are still checks running
            if (DateTimeOffset.UtcNow > _nextEmailSend
                && _listChecks.All(kp => kp.Value.Count == 0))
            {
                var user = await GetUser();
                if (user == null)
                {
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

                _nextEmailSend = ScanScheduling.GetNextEmailRunTime(
                    DateTimeOffset.UtcNow,
                    _marketHours
                );
                _container.AddNotice("Emails sent, next run at " + _marketHours.ToMarketTime(_nextEmailSend));
            }
        }

        private object ToEmailData(TriggeredAlert alert)
        {
            string FormattedValue()
            {
                return alert.valueType switch {
                    ValueFormat.Percentage => alert.triggeredValue.ToString("P1"),
                    ValueFormat.Currency => alert.triggeredValue.ToString("C2"),
                    ValueFormat.Number => alert.triggeredValue.ToString("N2"),
                    ValueFormat.Boolean => alert.triggeredValue.ToString(),
                    _ => throw new Exception("Unexpected alert value type: " + alert.valueType)
                };
            }

            return new {
                ticker = (string)alert.ticker,
                value = FormattedValue(),
                description = alert.description,
                time = _marketHours.ToMarketTime(alert.when).ToString("HH:mm") + " ET"
            };
        }

        private async Task<ServiceResponse<core.Shared.Adapters.Stocks.PriceBar[]>> GetPricesForTicker(
            UserState user,
            string ticker)
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
            if (!prices.IsOk)
            {
                _logger.LogCritical($"Could not get price history for {ticker}: {prices.Error.Message}");
            }
            return prices;
        }

        private async Task<ServiceResponse<StockQuote>> GetQuote(
            UserState user, string ticker)
        {
            var priceResponse = await _brokerage.GetQuote(user, ticker);
            if (!priceResponse.IsOk)
            {
                _logger.LogError($"Could not get price for {ticker}: {priceResponse.Error.Message}");
            }
            return priceResponse;
        }
    }
}