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
                var view = await _storage.ViewModel<StockDashboardView>(query.UserId);
                if (view == null)
                {
                    view = await LoadFromDb(query.UserId);
                }

                var tickers = view.Owned.Select(o => o.Ticker).Distinct();

                var tickerPrices = await _stocksService.GetPrices(tickers);

                return tickerPrices.IsOk switch
                {
                    false => view,
                    true => EnrichWithStockPrice(view, tickerPrices.Success)
                };
            }

            private StockDashboardView EnrichWithStockPrice(StockDashboardView view, Dictionary<string, BatchStockPrice> prices)
            {
                foreach (var o in view.Owned)
                {
                    prices.TryGetValue(o.Ticker, out var price);
                    o.ApplyPrice(price?.Price ?? 0);
                }

                return view;
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

                var positions = stocks.Where(s => s.State.Owned > 0)
                    .Select(s => s.State.CurrentPosition)
                    .OrderBy(p => p.Ticker)
                    .ToList();

                return new StockDashboardView(ownedStocks, positions);
            }
        }
    }
}