using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Alerts;
using core.Portfolio.Output;
using core.Shared;
using MediatR;

namespace core.Portfolio
{
    public class Get
    {
        public class Query : RequestWithUserId<PortfolioResponse>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler :
            HandlerWithStorage<Query, PortfolioResponse>,
            INotificationHandler<UserRecalculate>
        {
            private IStocksService2 _stocksService;
            private StockMonitorContainer _alerts;

            public Handler(
                IPortfolioStorage storage,
                IStocksService2 stockService,
                StockMonitorContainer alerts) : base(storage)
            {
                _stocksService = stockService;
                _alerts = alerts;
            }

            public override async Task<PortfolioResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var fromCache = await _storage.ViewModel<PortfolioResponse>(request.UserId);
                if (fromCache != null)
                {
                    return fromCache;
                }

                return await GetFromDatabase(request.UserId);
            }

            public async Task Handle(UserRecalculate notification, CancellationToken cancellationToken)
            {
                var data = await GetFromDatabase(notification.UserId);

                await _storage.SaveViewModel(notification.UserId, data);
            }

            private async Task<PortfolioResponse> GetFromDatabase(Guid userId)
            {
                var stocks = await _storage.GetStocks(userId);

                var owned = stocks.Where(s => s.State.Owned > 0);

                var options = await _storage.GetOwnedOptions(userId);

                var openOptions = options
                    .Where(o => o.State.NumberOfContracts != 0 && o.State.DaysUntilExpiration > -5)
                    .OrderBy(o => o.State.Expiration);

                var cryptos = await _storage.GetCryptos(userId);

                var obj = new PortfolioResponse
                {
                    OwnedStockCount = owned.Count(),
                    OpenOptionCount = openOptions.Count(),
                    OwnedCryptoCount = cryptos.Count(),
                    TriggeredAlertCount = _alerts.Monitors.Count(s => s.Alert.UserId == userId && s.IsTriggered),
                    AlertCount = _alerts.Monitors.Count(s => s.Alert.UserId == userId),
                    Calculated = DateTimeOffset.UtcNow
                };
                return obj;
            }
        }
    }
}