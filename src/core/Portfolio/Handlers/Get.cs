using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Alerts;
using core.Portfolio.Views;
using core.Shared;
using MediatR;

namespace core.Portfolio
{
    public class Get
    {
        public class Query : RequestWithUserId<PortfolioView>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler :
            HandlerWithStorage<Query, PortfolioView>,
            INotificationHandler<UserChanged>
        {
            private StockAlertContainer _alerts;

            public Handler(
                IPortfolioStorage storage,
                StockAlertContainer alerts) : base(storage)
            {
                _alerts = alerts;
            }

            public override async Task<PortfolioView> Handle(Query request, CancellationToken cancellationToken)
            {
                var fromCache = await _storage.ViewModel<PortfolioView>(request.UserId, PortfolioView.Version);
                if (fromCache != null)
                {
                    return fromCache;
                }

                return await GetFromDatabase(request.UserId);
            }

            public async Task Handle(UserChanged notification, CancellationToken cancellationToken)
            {
                var data = await GetFromDatabase(notification.UserId);

                await _storage.SaveViewModel(notification.UserId, data, PortfolioView.Version);
            }

            private async Task<PortfolioView> GetFromDatabase(Guid userId)
            {
                var stocks = await _storage.GetStocks(userId);

                var openStocks = stocks.Where(s => s.State.OpenPosition != null).Select(s => s.State.OpenPosition).ToList();

                var options = await _storage.GetOwnedOptions(userId);

                var openOptions = options
                    .Where(o => o.State.NumberOfContracts != 0 && o.State.DaysUntilExpiration > -5)
                    .OrderBy(o => o.State.Expiration);

                var cryptos = await _storage.GetCryptos(userId);

                var obj = new PortfolioView
                {
                    OpenStockCount = openStocks.Count(),
                    OpenOptionCount = openOptions.Count(),
                    OpenCryptoCount = cryptos.Count(),
                    Calculated = DateTimeOffset.UtcNow
                };
                return obj;
            }
        }
    }
}