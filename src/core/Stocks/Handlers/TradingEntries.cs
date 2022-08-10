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

                var brokerageOrders = await (user.State.ConnectedToBrokerage switch {
                    true => GetBrokerageOrders(_brokerage, user),
                    false => Task.FromResult(new BrokerageOrderView[0])
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

                var past = stocks
                    .SelectMany(s => s.State.PositionInstances.Where(t => t.IsClosed))
                    .OrderByDescending(p => p.Closed)
                    .ToArray();

                var performance = new TradingPerformanceContainerView(
                    new Span<PositionInstance>(past),
                    20
                );

                return new TradingEntriesView(
                    current: current,
                    past: past,
                    brokerageOrders: brokerageOrders,
                    performance: performance
                );
            }

            internal static async Task<BrokerageOrderView[]> GetBrokerageOrders(IBrokerage brokerage, User user)
            {
                var orders = await brokerage.GetOrders(user.State);
                return 
                    orders
                        .Where(o => o.IncludeInResponses)
                        .OrderBy(o => o.StatusOrder)
                        .Select(o => new BrokerageOrderView(
                            orderId: o.OrderId,
                            price: o.Price,
                            quantity: o.Quantity,
                            status: o.Status,
                            ticker: o.Ticker,
                            type: o.Type
                            )).ToArray();
            }
        }
    }
}