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
                var localPositions = view.Owned;
                var brokeragePositions = await _brokerage.GetPositions(state);
                var violations = new List<string>();

                // go through each position and see if it's recorded in portfolio, and quantity matches
                foreach (var brokeragePosition in brokeragePositions)
                {
                    var localPosition = localPositions.SingleOrDefault(o => o.Ticker == brokeragePosition.Ticker);
                    if (localPosition != null)
                    {
                        if (localPosition.Owned != brokeragePosition.Quantity)
                        {
                            violations.Add($"{brokeragePosition.Ticker} owned {brokeragePosition.Quantity} but NGTrading says {localPosition.Owned}");
                        }
                    }
                    else
                    {
                        violations.Add($"{brokeragePosition.Ticker} owned {brokeragePosition.Quantity} @ ${brokeragePosition.AveragePrice} but NGTrading says none");
                    }
                }

                // go through each portfolion and see if it's in positions
                foreach (var localPosition in localPositions)
                {
                    var brokeragePosition = brokeragePositions.SingleOrDefault(p => p.Ticker == localPosition.Ticker);
                    if (brokeragePosition == null)
                    {
                        violations.Add($"{localPosition.Ticker} owned {localPosition.Owned} but TDAmeritrade says none");
                    }
                    else
                    {
                        if (brokeragePosition.Quantity != localPosition.Owned)
                        {
                            violations.Add($"{localPosition.Ticker} owned {localPosition.Owned} but TDAmeritrade says {brokeragePosition.Quantity}");
                        }
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