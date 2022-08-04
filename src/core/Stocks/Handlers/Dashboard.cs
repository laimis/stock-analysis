using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared;
using core.Shared.Adapters.Brokerage;
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
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;
            private IStocksService2 _stocksService;

            public Handler(
                IAccountStorage accounts,
                IBrokerage brokerage,
                IPortfolioStorage storage,
                IStocksService2 stockService) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
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
                if (tickerPrices.IsOk)
                {
                    EnrichWithStockPrice(view, tickerPrices.Success);
                }

                var user = await _accounts.GetUser(query.UserId);
                if (user.State.ConnectedToBrokerage)
                {
                    await EnrichWithPositionCheckAsync(view, user.State);
                }

                return view;
            }

            private async Task EnrichWithPositionCheckAsync(StockDashboardView view, UserState state)
            {
                var positions = await _brokerage.GetPositions(state);
                var violations = new List<string>();

                // go through each position and see if it's recorded in portfolio, and quantity matches
                foreach (var position in positions)
                {
                    var existing = view.Owned.SingleOrDefault(o => o.Ticker == position.Ticker);
                    if (existing != null)
                    {
                        if (existing.Owned != position.Quantity)
                        {
                            violations.Add($"{position.Ticker} owned {position.Quantity} but NGTrading says {existing.Owned}");
                        }
                    }
                    else
                    {
                        violations.Add($"{position.Ticker} owned {position.Quantity} but NGTrading says none");
                    }
                }

                // go through each portfolion and see if it's in positions
                foreach (var portfolio in view.Owned)
                {
                    var existing = positions.SingleOrDefault(p => p.Ticker == portfolio.Ticker);
                    if (existing == null)
                    {
                        violations.Add($"{portfolio.Ticker} owned {portfolio.Owned} but TDAmeritrade says none");
                    }
                }

                view.Violations = violations;
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