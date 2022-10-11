using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Adapters.Emails;
using core.Adapters.Stocks;
using core.Alerts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using web.Utils;

namespace web.BackgroundServices
{
    public class StockMonitorService : BackgroundService
    {
        private IAccountStorage _accounts;
        private ILogger<StockMonitorService> _logger;
        private IEmailService _emails;
        private IStocksService2 _stocks;
        private IPortfolioStorage _stockStorage;
        private MarketHours _marketHours;
        public StockMonitorContainer _container;

        public IEnumerable<StockPositionMonitor> Monitors => _container.Monitors;

        public StockMonitorService(
            ILogger<StockMonitorService> logger,
            IAccountStorage accounts,
            IPortfolioStorage stockStorage,
            IStocksService2 stocks,
            IEmailService emails,
            MarketHours marketHours,
            StockMonitorContainer container)
        {
            _accounts = accounts;
            _emails = emails;
            _logger = logger;
            _stocks = stocks;
            _stockStorage = stockStorage;
            _marketHours = marketHours;
            _container = container;
        }

        

        private const int LONG_INTERVAL = 60_000; // one minute
        private const int SHORT_INTERVAL = 10_000; // 10 seconds

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

                    await Task.Delay(LONG_INTERVAL, stoppingToken);
                }
            }

            _logger.LogInformation("exec exit");
        }

        private async Task Loop(CancellationToken stoppingToken)
        {
            var time = DateTimeOffset.UtcNow;
            if (_marketHours.IsOn(time))
            {
                await ScanAlerts();

                await Task.Delay(LONG_INTERVAL, stoppingToken);
            }
            else
            {
                await Task.Delay(SHORT_INTERVAL, stoppingToken);
            }
        }

        private async Task BuildUpAlerts()
        {
            var users = await _accounts.GetUserEmailIdPairs();

            foreach (var pair in users)
            {
                var stocks = await _stockStorage.GetStocks(new Guid(pair.id));

                var open = stocks.Where(s => s.State.OpenPosition != null);

                foreach(var stock in open)
                {
                    _container.Register(stock);
                }
            }
        }

        private async Task ScanAlerts()
        {
            var triggered = new List<StockMonitorTrigger>();

            foreach (var t in _container.GetTickers())
            {
                var price = await _stocks.GetPrice(t);

                if (!price.IsOk || price.Success.NotFound)
                {
                    _logger.LogError($"price not found for {t}");
                    continue;
                }

                foreach(var trigger in _container.UpdateValue(t, price.Success.Amount, DateTimeOffset.UtcNow))
                {
                    triggered.Add(trigger);
                }
            }

            var grouped = triggered.GroupBy(t => t.UserId);

            foreach (var e in grouped)
            {
                var u = await _accounts.GetUser(e.Key);

                var alerts = e.ToList();

                var data = new { alerts = alerts.Select(Map) };

                await _emails.Send(
                    new Recipient(email: u.State.Email, name: u.State.Name),
                    Sender.NoReply,
                    EmailTemplate.Alerts,
                    data
                );
            }
        }

        private object Map(StockMonitorTrigger trigger)
        {
            return new {
                ticker = (string)trigger.Ticker,
                value = trigger.Value,
                description = trigger.Ticker,
                direction = $"Stop price alert hit for {trigger.Ticker} at {trigger.Value}",
                time = _marketHours.ToMarketTime(trigger.When).ToString("HH:mm") + " ET"
            };
        }
    }
}