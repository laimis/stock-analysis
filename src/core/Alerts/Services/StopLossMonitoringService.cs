using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Storage;

namespace core.Alerts.Services
{
    public class StopLossMonitoringService
    {
        private IAccountStorage _accounts;
        private IBrokerage _brokerage;
        private StockAlertContainer _container;
        private IPortfolioStorage _portfolioStorage;

        public StopLossMonitoringService(
            IAccountStorage accounts,
            IBrokerage brokerage,
            StockAlertContainer container,
            IPortfolioStorage portfolioStorage)
        {
            _accounts = accounts;
            _brokerage = brokerage;
            _container = container;
            _portfolioStorage = portfolioStorage;
        }

        // stop loss should be monitored at the following times:
        // on trading days every 5 minutes from 9:45am to 3:30pm
        // and no monitoring on weekends

        private static readonly TimeOnly _marketStartTime = new TimeOnly(9, 30, 0);
        private static readonly TimeOnly _marketEndTime = new TimeOnly(16, 0, 0);

        public static DateTimeOffset CalculateNextRunDateTime(DateTimeOffset now, IMarketHours marketHours)
        {
            var eastern = marketHours.ToMarketTime(now);
            var marketStartTimeInEastern = eastern.Date.Add(_marketStartTime.ToTimeSpan());

            var nextScan = TimeOnly.FromTimeSpan(eastern.TimeOfDay) switch {
                var t when t < _marketStartTime => marketStartTimeInEastern,
                var t when t > _marketEndTime => marketStartTimeInEastern.AddDays(1).AddMinutes(15),
                _ => eastern.AddMinutes(5)
            };

            // if the next scan is on a weekend, let's skip those days
            if (nextScan.DayOfWeek == DayOfWeek.Saturday)
            {
                nextScan = nextScan.AddDays(2);
            }
            else if (nextScan.DayOfWeek == DayOfWeek.Sunday)
            {
                nextScan = nextScan.AddDays(1);
            }

            return marketHours.ToUniversalTime(nextScan);
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            _container.ToggleStopLossCheckCompleted(false);

            var users = await _accounts.GetUserEmailIdPairs();

            foreach(var (_, userId) in users)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                LogInformation($"Running stop loss check for {userId}");

                var user = await _accounts.GetUser(new Guid(userId));
                if (user == null)
                {
                    LogError($"Unable to find user {userId}");
                    continue;
                }

                var checks = (await _portfolioStorage.GetStocks(user.Id))
                    .Where(s => s.State.OpenPosition != null)
                    .Select(s => s.State.OpenPosition)
                    .Where(p => p.StopPrice != null)
                    .ToList();

                foreach(var c in checks)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var priceResponse = await _brokerage.GetQuote(user.State, c.Ticker);
                    if (!priceResponse.IsOk)
                    {
                        LogError("Could not get price for {ticker}: {message}", c.Ticker, priceResponse.Error.Message);
                        continue;
                    }

                    var price = priceResponse.Success.Price;
                    if (price <= c.StopPrice.Value)
                    {
                        StopPriceMonitor.Register(
                            container: _container,
                            price: price,
                            stopPrice: c.StopPrice.Value,
                            ticker: c.Ticker,
                            when: DateTimeOffset.UtcNow,
                            userId: user.State.Id
                        );
                    }
                    else
                    {
                        StopPriceMonitor.Deregister(_container, c.Ticker, user.State.Id);
                    }
                }
            }

            _container.ToggleStopLossCheckCompleted(true);
        }

        private void LogError(string message, params object[] args)
        {
            // TODO: pick implementation direction
        }

        private void LogInformation(string message)
        {
            // TODO: pick implementation direction
        }
    }
}