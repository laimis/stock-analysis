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
        private IMarketHours _marketHours;
        public StockMonitorContainer _container;

        // TODO: can this be removed?
        public IEnumerable<IStockPositionMonitor> Monitors => _container.Monitors;

        public StockMonitorService(
            ILogger<StockMonitorService> logger,
            IAccountStorage accounts,
            IPortfolioStorage stockStorage,
            IStocksService2 stocks,
            IEmailService emails,
            IMarketHours marketHours,
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

            await BuildUpAlerts();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
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
            var triggered = new List<TriggeredAlert>();

            foreach (var t in _container.GetTickers())
            {
                var price = await _stocks.GetPrice(t);

                if (!price.IsOk || price.Success.NotFound)
                {
                    _logger.LogError($"price not found for {t}");
                    continue;
                }

                foreach(var trigger in _container.RunCheck(t, price.Success.Amount, DateTimeOffset.UtcNow))
                {
                    triggered.Add(trigger);
                }
            }

            var grouped = triggered.GroupBy(t => t.userId);

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

        private object Map(TriggeredAlert trigger)
        {
            return new {
                ticker = (string)trigger.ticker,
                value = trigger.triggeredValue,
                description = trigger.description,
                direction = trigger.description,
                time = _marketHours.ToMarketTime(trigger.when).ToString("HH:mm") + " ET"
            };
        }
    }
}