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
using core.Shared.Adapters.SMS;
using core.Shared.Adapters.Stocks;
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
        private ISMSClient _sms;

        public StockAlertService(
            IAccountStorage accounts,
            IBrokerage brokerage,
            StockAlertContainer container,
            IEmailService emails,
            ILogger<StockAlertService> logger,
            IMarketHours marketHours,
            IPortfolioStorage portfolio,
            ISMSClient sms)
        {
            _accounts = accounts;
            _brokerage = brokerage;
            _container = container;
            _emails = emails;
            _logger = logger;
            _marketHours = marketHours;
            _portfolio = portfolio;
            _sms = sms;
        }

        private Dictionary<string, List<AlertCheck>> _listChecks = new Dictionary<string, List<AlertCheck>>();
        private bool _listChecksFinished = false;
        private DateTimeOffset _nextListMonitoringRun = DateTimeOffset.MinValue;
        private DateTimeOffset _nextStopLossCheck = DateTimeOffset.MinValue;
        private bool _stopLossCheckFinished = false;
        private DateTimeOffset _nextEmailSend = DateTimeOffset.MinValue;
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _container.AddNotice("Stock alert service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {    
                try
                {
                    var user = new Lazy<Task<UserState>>(() => GetUser());

                    await RunThroughListMonitoringChecks(user, stoppingToken);

                    await RunThroughStopLossChecks(user, stoppingToken);

                    await SendAlertSummaryEmail(user);

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed while running alert monitor, will sleep");
                    _container.AddNotice("Failed while running alert monitor: " + ex.Message);
                    _container.ManualRunRequested();
                    await Task.Delay(TimeSpan.FromMinutes(10));
                }
            }
        }

        private async Task RunThroughStopLossChecks(Lazy<Task<UserState>> userFunc, CancellationToken cancellationToken)
        {
            if (DateTimeOffset.UtcNow > _nextStopLossCheck)
            {
                var user = await userFunc.Value;
                if (user == null)
                {
                    return;
                }

                var checks = await AlertCheckGenerator.GetStopLossChecks(_portfolio, user);

                var completed = await core.Alerts.Services.Monitors.MonitorForStopLosses(
                    quoteFunc: (u, ticker) => GetQuote(u, ticker),
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

                    _stopLossCheckFinished = true;
                }

                _container.AddNotice($"Completed {completed.Count} out of {checks.Count} stop loss checks, next run at {_marketHours.ToMarketTime(_nextStopLossCheck)}");
            }
        }

        private async Task RunThroughListMonitoringChecks(Lazy<Task<UserState>> user, CancellationToken stoppingToken)
        {
            if (DateTimeOffset.UtcNow > _nextListMonitoringRun || _container.ManualRunRequested())
            {
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

            _listChecksFinished = _listChecks.All(kp => kp.Value.Count == 0);
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

        private async Task SendAlertSummaryEmail(Lazy<Task<UserState>> userFunc)
        {
            // wait to send emails if there are still checks running or stop checks haven't run
            if (DateTimeOffset.UtcNow > _nextEmailSend
                && _listChecksFinished && _stopLossCheckFinished)
            {
                var user = await userFunc.Value;
                if (user == null)
                {
                    return;
                }

                // get all alerts for that user
                var alertGroups = _container.GetAlerts(user.Id)
                    .GroupBy(a => a.identifier)
                    .Select(ToAlertEmailGroup);

                var data = new { alertGroups };

                await _emails.Send(
                    new Recipient(email: user.Email, name: user.Name),
                    Sender.NoReply,
                    EmailTemplate.Alerts,
                    data
                );

                // if there are any stop alerts, send sms
                var tickersWithStopAlert = _container
                        .GetAlerts(user.Id)
                        .Where(a => a.identifier == StopPriceMonitor.Identifier)
                        .Select(a => a.ticker);

                var stopAlertString = string.Join(", ", tickersWithStopAlert);

                if (!string.IsNullOrEmpty(stopAlertString))
                {
                    var message = $"Stop loss triggered for {stopAlertString}";

                    await _sms.SendSMS(message);
                }

                _nextEmailSend = ScanScheduling.GetNextEmailRunTime(
                    DateTimeOffset.UtcNow,
                    _marketHours
                );
                _container.AddNotice("Emails sent, next run at " + _marketHours.ToMarketTime(_nextEmailSend));
            }
        }

        private object ToAlertEmailGroup(IGrouping<string, TriggeredAlert> group)
        {
            return new {
                identifier = group.Key,
                alerts = group.Select(ToEmailData)
            };
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

        
        private class GetPricesForTickerService
        {
            private IBrokerage _brokerage;
            private IMarketHours _marketHours;
            private ILogger _logger;
            private Lazy<Task<UserState>> _user;
            private Dictionary<string, ServiceResponse<PriceBar[]>> _cache =
                new Dictionary<string, ServiceResponse<PriceBar[]>>();

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
                if (_cache.ContainsKey(ticker))
                {
                    return _cache[ticker];
                }

                var start = _marketHours.GetMarketStartOfDayTimeInUtc(DateTime.UtcNow.AddDays(-365));
                var end = _marketHours.GetMarketEndOfDayTimeInUtc(DateTime.UtcNow);
                var user = await _user.Value;
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
                _logger.LogError($"Could not get price for {ticker}: {priceResponse.Error.Message}");
            }
            return priceResponse;
        }
    }
}