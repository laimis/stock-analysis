using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Alerts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web
{
    internal class TestService : BackgroundService
    {
        private IAccountStorage _accounts;
        private ILogger<TestService> _logger;
        private IAlertsStorage _alerts;
        private IStocksService2 _stocks;

        public TestService(
            ILogger<TestService> logger,
            IAccountStorage accounts,
            IAlertsStorage alerts,
            IStocksService2 stocks)
        {
            _accounts = accounts;
            _alerts = alerts;
            _logger = logger;
            _stocks = stocks;
        }

        private HashSet<string> _tickers = new HashSet<string>();
        private Dictionary<Guid, StockMonitor> _monitors = new Dictionary<Guid, StockMonitor>();

        private const int INTERVAL = 3_000;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("exec enter");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("loop " + DateTime.UtcNow);

                var users = await _accounts.GetUserEmailIdPairs();

                foreach(var pair in users)
                {
                    var alerts = await _alerts.GetAlerts(new Guid(pair.id));

                    foreach(var a in alerts)
                    {
                        _tickers.Add(a.State.Ticker);

                        if (!_monitors.ContainsKey(a.Id))
                        {
                            _monitors[a.Id] = new StockMonitor(a);
                        }
                    }
                }

                foreach(var t in _tickers)
                {
                    var price = await _stocks.GetPrice(t);

                    if (price.NotFound)
                    {
                        _logger.LogError($"price not found for {t}");
                    }

                    _logger.LogDebug($"price {t} {price}");

                    foreach(var p in _monitors)
                    {
                        var triggered = p.Value.UpdateValue(t, price.Amount);
                    }
                }

                await Task.Delay(INTERVAL, stoppingToken);
            }

            _logger.LogDebug("exec exit");
        }
    }
}