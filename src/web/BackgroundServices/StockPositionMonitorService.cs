using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Adapters.Emails;
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
        private IPortfolioStorage _stockStorage;
        private IMarketHours _marketHours;
        public StockMonitorContainer _container;

        public StockPositionMonitorService(
            ILogger<StockPositionMonitorService> logger,
            IAccountStorage accounts,
            IBrokerage brokerage,
            IPortfolioStorage stockStorage,
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
            _stockStorage = stockStorage;
            _marketHours = marketHours;
            _container = container;
        }

        private const int LONG_INTERVAL = 60_000; // one minute
        private const int SHORT_INTERVAL = 10_000; // 10 seconds

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("stock position monitoring service started");

            var firstRun = true;
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (firstRun)
                    {
                        await BuildUpAlerts();
                    }

                    await Loop(firstRun, stoppingToken);
                    
                    firstRun = false;
                }
                catch(Exception ex)
                {
                    _logger.LogError("Stock position monitoring failed:" + ex);

                    await Task.Delay(LONG_INTERVAL, stoppingToken);
                }
            }

            _logger.LogInformation("stock position monitoring service stopped");
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
                    _logger.LogInformation($"adding {stock.State.Ticker} to monitor for {pair.email}");
                    _container.Register(stock.State);
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
                if (price.NotFound)
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

            var userAlerts = triggered.GroupBy(t => t.userId);

            foreach (var userAlertGroup in userAlerts)
            {
                var user = await _accounts.GetUser(userAlertGroup.Key);
                
                await SendEmails(userAlertGroup, user.State);

                await SendSMS(userAlertGroup, user.State);
            }
        }

        private async Task SendSMS(IGrouping<Guid, TriggeredAlert> userAlertGroup, UserState state)
        {
            var notRecentlyTriggered = userAlertGroup
                .Where(a => !_container.HasRecentlyTriggered(a));
                
            var groupedBySource = notRecentlyTriggered.GroupBy(a => a.source);

            foreach (var group in groupedBySource)
            {
                var alert = $"Found {group.Count()} {group.First().source} alerts: {string.Join(", ", group.Select(a => a.ticker))}";
                await _smsClient.SendSMS(alert);
            }
        }

        private async Task SendEmails(IGrouping<Guid, TriggeredAlert> userAlertGroup, UserState user)
        {
            var data = new {
                alerts = userAlertGroup.Select(ToEmailData)
            };

            await _emails.Send(
                new Recipient(email: user.Email, name: user.Name),
                Sender.NoReply,
                EmailTemplate.Alerts,
                data
            );
        }

        private async Task<Price> GetPrice(Dictionary<string, Price> priceCache, PriceMonitor sm)
        {
            if (priceCache.ContainsKey(sm.Ticker))
            {
                return priceCache[sm.Ticker];
            }

            var user = await _accounts.GetUser(sm.UserId);
            if (user == null)
            {
                _logger.LogError($"user not found for {sm.UserId} while getting price for {sm.Ticker}");
                return Price.Failed;
            }
            
            var price = await _brokerage.GetQuote(user.State, sm.Ticker);
            if (!price.IsOk)
            {
                _logger.LogError($"failed to get price for {sm.Ticker} for {sm.UserId}: {price.Error}");
                return Price.Failed;
            }

            priceCache[sm.Ticker] = new Price(price.Success.lastPrice);

            return priceCache[sm.Ticker];
        }

        private object ToEmailData(TriggeredAlert trigger)
        {
            return new {
                ticker = (string)trigger.ticker,
                value = trigger.triggeredValue,
                description = trigger.description,
                time = _marketHours.ToMarketTime(trigger.when).ToString("HH:mm") + " ET"
            };
        }
    }
}