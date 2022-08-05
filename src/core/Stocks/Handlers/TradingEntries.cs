using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared.Adapters.Brokerage;
using core.Stocks.View;
using MediatR;

namespace core.Stocks
{
    public class TradingEntries
    {
        public class Query : IRequest<TradingEntriesView>
        {
            public Query(Guid userId)
            {
                UserId = userId;
            }

            public Guid UserId { get; }
        }

        public class Handler : IRequestHandler<Query, TradingEntriesView>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;
            private IPortfolioStorage _portfolio;
            private IStocksService2 _stocks;

            public Handler(
                IAccountStorage accounts,
                IBrokerage brokerage,
                IPortfolioStorage portfolio,
                IStocksService2 stocks)
            {
                _accounts = accounts;
                _brokerage = brokerage;
                _portfolio = portfolio;
                _stocks = stocks;
            }

            public async Task<TradingEntriesView> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _portfolio.GetStocks(request.UserId);

                var user = await _accounts.GetUser(request.UserId);

                var pendingOrders = await (user.State.ConnectedToBrokerage switch {
                    true => GetPendingOrdersAsync(user),
                    false => Task.FromResult(new PendingOrderView[0])
                });

                var tradingEntries = stocks.Where(s => s.State.Owned > 0 && s.State.Category == StockCategory.ShortTerm)
                    .Select(s => new TradingEntryView(s.State))
                    .ToArray();

                var prices = await _stocks.GetPrices(tradingEntries.Select(s => s.Ticker).Distinct());
                if (prices.IsOk)
                {
                    foreach (var entry in tradingEntries)
                    {
                        prices.Success.TryGetValue(entry.Ticker, out var price);
                        entry.ApplyPrice(price?.Price ?? 0);    
                    }   
                }

                var current = tradingEntries
                    .OrderByDescending(s => s.Gain)
                    .ToArray();

                var closedPositions = stocks
                    .SelectMany(s => s.State.PositionInstances.Where(t => t.IsClosed))
                    .ToList();

                var past = closedPositions
                    .OrderByDescending(p => p.Closed)
                    .ToArray();

                var performance = new TradingPerformanceContainerView(
                    closedPositions,
                    20
                );

                return new TradingEntriesView(
                    current: current,
                    past: past,
                    pendingOrders: pendingOrders,
                    performance: performance
                );
            }

            private async Task<PendingOrderView[]> GetPendingOrdersAsync(User user)
            {
                var orders = await _brokerage.GetPendingOrders(user.State);
                return orders.Select(o => new PendingOrderView(
                    orderId: o.OrderId,
                    price: o.Price,
                    quantity: o.Quantity,
                    ticker: o.Ticker,
                    type: o.Type
                    )).ToArray();
            }
        }
    }
}