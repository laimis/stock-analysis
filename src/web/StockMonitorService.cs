using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Emails;
using core.Adapters.Stocks;
using core.Alerts;
using core.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web
{
    internal class StockMonitorService : BackgroundService
    {
        private IAccountStorage _accounts;
        private ILogger<StockMonitorService> _logger;
        private IAlertsStorage _alerts;
        private IEmailService _emails;
        private IStocksService2 _stocks;
        private MarketHours _marketHours;

        public StockMonitorService(
            ILogger<StockMonitorService> logger,
            IAccountStorage accounts,
            IAlertsStorage alerts,
            IStocksService2 stocks,
            IEmailService emails,
            MarketHours marketHours)
        {
            _accounts = accounts;
            _alerts = alerts;
            _emails = emails;
            _logger = logger;
            _stocks = stocks;
            _marketHours = marketHours;
        }

        private HashSet<string> _tickers = new HashSet<string>();
        public static Dictionary<Guid, StockMonitor> _monitors = new Dictionary<Guid, StockMonitor>();

        private const int LONG_INTERVAL = 60_000 * 15;
        private const int SHORT_INTERVAL = 10_000;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("exec enter");

            while (!stoppingToken.IsCancellationRequested)
            {
                var time = DateTimeOffset.UtcNow;

                if (_marketHours.IsOn(time))
                {
                    _logger.LogInformation($"market hours {time.TimeOfDay}");

                    try
                    {
                        await ScanAlerts();
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError("Failed:" + ex);
                    }

                    await Task.Delay(LONG_INTERVAL, stoppingToken);
                }
                else
                {
                    _logger.LogInformation($"non market hours {time.TimeOfDay}");

                    await Task.Delay(SHORT_INTERVAL, stoppingToken);
                }
            }

            _logger.LogInformation("exec exit");
        }

        private async Task ScanAlerts()
        {
            var users = await _accounts.GetUserEmailIdPairs();

            foreach (var pair in users)
            {
                var alerts = await _alerts.GetAlerts(new Guid(pair.id));

                foreach (var a in alerts)
                {
                    _tickers.Add(a.State.Ticker);

                    foreach(var pp in a.PricePoints)
                    {
                        if (!_monitors.ContainsKey(pp.Id))
                        {
                            _monitors[pp.Id] = new StockMonitor(a, pp);
                        }
                    }
                }
            }

            var triggered = new List<StockMonitorTrigger>();

            foreach (var t in _tickers)
            {
                var price = await _stocks.GetPrice(t);

                if (price.NotFound)
                {
                    _logger.LogError($"price not found for {t}");
                    continue;
                }

                foreach (var m in _monitors.Values.ToList())
                {
                    if (m.UpdateValue(t, price.Amount))
                    {
                        triggered.Add(new StockMonitorTrigger(m.Alert, price, DateTimeOffset.UtcNow));
                    }
                }
            }

            var grouped = triggered.GroupBy(t => t.Alert.State.UserId);

            foreach (var e in grouped)
            {
                var u = await _accounts.GetUser(e.Key);

                var alerts = e.OrderByDescending(m => m.Price.Amount).ToList();

                var data = new { alerts = alerts.Select(Map) };

                await _emails.Send(u.Email, Sender.NoReply, EmailTemplate.Alerts, data);
            }
        }

        private object Map(StockMonitorTrigger trigger)
        {
            return new {
                ticker = (string)trigger.Alert.State.Ticker,
                value = trigger.Price.Amount,
                time = _marketHours.ToMarketTime(trigger.When).ToString("HH:mm") + " ET"
            };
        }
    }
}