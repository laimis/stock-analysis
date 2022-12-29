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
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.SMS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class StockPositionMonitorService : BackgroundService
    {
        private IAccountStorage _accounts;
        private IBrokerage _brokerage;
        private ILogger<StockPositionMonitorService> _logger;
        private ISMSClient _smsClient;
        private IEmailService _emails;
        private IStocksService2 _stocks;
        private IPortfolioStorage _stockStorage;
        private IMarketHours _marketHours;
        public StockMonitorContainer _container;

        public StockPositionMonitorService(
            ILogger<StockPositionMonitorService> logger,
            IAccountStorage accounts,
            IBrokerage brokerage,
            IPortfolioStorage stockStorage,
            IStocksService2 stocks,
            IEmailService emails,
            ISMSClient smsClient,
            IMarketHours marketHours,
            StockMonitorContainer container)
        {
            _accounts = accounts;
            _brokerage = brokerage;
            _emails = emails;
            _logger = logger;
            _smsClient = smsClient;
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

            var firstRun = true;
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Loop(firstRun, stoppingToken);
                    
                    firstRun = false;
                }
                catch(Exception ex)
                {
                    _logger.LogError("Failed:" + ex);

                    await Task.Delay(LONG_INTERVAL, stoppingToken);
                }
            }

            _logger.LogInformation("exec exit");
        }

        private async Task Loop(bool firstRun, CancellationToken stoppingToken)
        {
            var time = DateTimeOffset.UtcNow;
            if (_marketHours.IsMarketOpen(time))
            {
                await ScanAlerts(sendAlerts:!firstRun);

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

        private async Task ScanAlerts(bool sendAlerts)
        {
            var triggered = new List<TriggeredAlert>();
            var priceCache = new Dictionary<string, Price>();

            foreach (var m in _container.Monitors)
            {
                var priceTask = m switch {
                    PriceMonitor sm => GetPrice(priceCache, sm),
                    _ => Task.FromResult(new Price(m.LastSeenValue))
                };

                var price = await priceTask;
                
                if (!price.NotFound)
                {
                    _logger.LogError($"price not found for {m.Description} monitor");
                    continue;
                }

                if (m.RunCheck(price.Amount, DateTimeOffset.UtcNow))
                {
                    triggered.Add(m.TriggeredAlert.Value);
                    _container.AddToRecent(m.TriggeredAlert.Value);
                }
            }

            if (!sendAlerts)
            {
                return;
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

                foreach(var a in alerts)
                {
                    // only send alerts via SMS if they haven't recently been hit
                    if (_container.HasRecentlyTriggered(a))
                    {
                        continue;
                    }

                    await _smsClient.SendSMS(a.description);
                }
            }
        }

        private async Task<Price> GetPrice(Dictionary<string, Price> priceCache, PriceMonitor sm)
        {
            if (priceCache.ContainsKey(sm.Ticker))
            {
                return priceCache[sm.Ticker];
            }

            var user = await _accounts.GetUser(sm.UserId);
            var price = await _brokerage.GetQuote(user.State, sm.Ticker);

            priceCache[sm.Ticker] = new Price(price.Success.lastPrice);

            return priceCache[sm.Ticker];
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