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
    public class StockMonitorService : BackgroundService
    {
        private IAccountStorage _accounts;
        private ILogger<StockMonitorService> _logger;
        private IAlertsStorage _alerts;
        private IEmailService _emails;
        private IStocksService2 _stocks;
        private MarketHours _marketHours;
        public StockMonitorContainer _container;

        public IEnumerable<StockMonitor> Monitors => _container.Monitors;

        public StockMonitorService(
            ILogger<StockMonitorService> logger,
            IAccountStorage accounts,
            IAlertsStorage alerts,
            IStocksService2 stocks,
            IEmailService emails,
            MarketHours marketHours,
            StockMonitorContainer container)
        {
            _accounts = accounts;
            _alerts = alerts;
            _emails = emails;
            _logger = logger;
            _stocks = stocks;
            _marketHours = marketHours;
            _container = container;
        }

        

        private const int LONG_INTERVAL = 60_000 * 15;
        private const int SHORT_INTERVAL = 10_000;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("exec enter");

            var firstRun = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await BuildUpAlerts();

                    if (firstRun)
                    {
                        _logger.LogInformation("First run scan");
                        
                        await ScanAlerts();

                        firstRun = false;
                    }

                    await Loop(stoppingToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError("Failed:" + ex);
                }
            }

            _logger.LogInformation("exec exit");
        }

        private async Task Loop(CancellationToken stoppingToken)
        {
            var time = DateTimeOffset.UtcNow;
            if (_marketHours.IsOn(time))
            {
                _logger.LogInformation($"market hours {time.TimeOfDay}");

                await ScanAlerts();

                await Task.Delay(LONG_INTERVAL, stoppingToken);
            }
            else
            {
                _logger.LogInformation($"non market hours {time.TimeOfDay}");

                await Task.Delay(SHORT_INTERVAL, stoppingToken);
            }
        }

        private async Task BuildUpAlerts()
        {
            var users = await _accounts.GetUserEmailIdPairs();

            foreach (var pair in users)
            {
                var alerts = await _alerts.GetAlerts(new Guid(pair.id));

                foreach (var a in alerts)
                {
                    _container.Register(a);
                }
            }
        }

        private async Task ScanAlerts()
        {
            var triggered = new List<StockMonitorTrigger>();

            foreach (var t in _container.GetTickers())
            {
                var price = await _stocks.GetPrice(t);

                if (price.NotFound)
                {
                    _logger.LogError($"price not found for {t}");
                    continue;
                }

                foreach(var trigger in _container.UpdateValue(t, price.Amount, DateTimeOffset.UtcNow))
                {
                    triggered.Add(trigger);
                }
            }

            var grouped = triggered.GroupBy(t => t.UserId);

            foreach (var e in grouped)
            {
                var u = await _accounts.GetUser(e.Key);

                var alerts = e.OrderByDescending(m => m.NewValue).ToList();

                var data = new { alerts = alerts.Select(Map) };

                await _emails.Send(u.Email, Sender.NoReply, EmailTemplate.Alerts, data);
            }
        }

        private object Map(StockMonitorTrigger trigger)
        {
            return new {
                ticker = (string)trigger.Ticker,
                value = trigger.NewValue,
                description = trigger.Monitor.PricePoint.Description,
                direction = $"crossed <b>{trigger.Direction}</b> through {trigger.Monitor.PricePoint.Value}",
                time = _marketHours.ToMarketTime(trigger.When).ToString("HH:mm") + " ET"
            };
        }
    }
}