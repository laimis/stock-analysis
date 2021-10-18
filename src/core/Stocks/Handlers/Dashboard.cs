using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared;
using core.Stocks.View;
using MediatR;

namespace core.Stocks
{
    public class Dashboard
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, object>,
            INotificationHandler<UserChanged>
        {
            private IStocksService2 _stocksService;

            public Handler(IPortfolioStorage storage, IStocksService2 stockService) : base(storage)
            {
                _stocksService = stockService;
            }

            public override async Task<object> Handle(Query query, CancellationToken cancellationToken)
            {
                var cached = await _storage.ViewModel<StockDashboardView>(query.UserId);
                if (cached == null)
                {
                    cached = await LoadFromDb(query.UserId);
                }

                var tickers = cached.Owned.Select(o => o.Ticker).Distinct().ToList();

                var tickerPrices = await _stocksService.GetPrices(tickers);

                foreach (var o in cached.Owned)
                {
                    tickerPrices.TryGetValue(o.Ticker, out var price);
                    o.ApplyPrice(price?.Price ?? 0);
                }

                return cached;
            }

            public async Task Handle(UserChanged notification, CancellationToken cancellationToken)
            {
                var fromDb = await LoadFromDb(notification.UserId);

                await _storage.SaveViewModel(notification.UserId, fromDb);
            }

            private async Task<StockDashboardView> LoadFromDb(Guid userId)
            {
                var stocks = await _storage.GetStocks(userId);

                var ownedStocks = stocks
                    .Where(s => s.State.Owned > 0)
                    .Select(s => new OwnedStockView(s))
                    .ToList();

                var closedTransactions = stocks
                    .SelectMany(s => s.State.PositionInstances.Where(t => t.IsClosed))
                    .ToList();

                var performance = new StockOwnershipPerformanceContainerView(
                    closedTransactions
                );

                var past = closedTransactions
                    .Select(t => new PositionInstanceView(t))
                    .OrderByDescending(p => p.Date)
                    .ToList();

                var obj = new StockDashboardView
                {
                    Owned = ownedStocks,
                    Performance = performance,
                    Past = past,
                    Calculated = DateTimeOffset.UtcNow
                };
                return obj;
            }
        }
    }
}