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
                    false => Task.FromResult(new Order[0])
                });

                var brokeragePositions = await (user.State.ConnectedToBrokerage switch {
                    true => GetBrokeragePositions(_brokerage, user),
                    false => Task.FromResult<IEnumerable<Position>>(new Position[0])
                });

                var positions = stocks.Where(s => s.State.OpenPosition != null)
                    .Select(s => s.State.OpenPosition)
                    .ToArray();

                var prices = await _stocks.GetPrices(positions.Select(s => s.Ticker).Distinct());
                if (prices.IsOk)
                {
                    foreach (var entry in positions)
                    {
                        prices.Success.TryGetValue(entry.Ticker, out var price);
                        entry.SetPrice(price?.Price ?? 0);    
                    }   
                }

                var current = positions
                    .OrderByDescending(s => s.RR)
                    .ToArray();

                var past = stocks
                    .SelectMany(s => s.State.Positions)
                    .Where(s => s.IsClosed)
                    .OrderByDescending(p => p.Closed)
                    .ToArray();

                var performance = new TradingPerformanceContainerView(
                    new Span<PositionInstance>(past),
                    20
                );

                var violations = Dashboard.Handler.GetViolations(brokeragePositions, positions);

                return new TradingEntriesView(
                    current: current,
                    past: past,
                    brokerageOrders: brokerageOrders,
                    performance: performance,
                    violations: violations
                );
            }

            private static async Task<IEnumerable<Position>> GetBrokeragePositions(IBrokerage brokerage, User user)
            {
                var positions = await brokerage.GetPositions(user.State);

                return positions.IsOk switch {
                    true => positions.Success,
                    false => new Position[0]
                };
            }   
            
            internal static async Task<Order[]> GetBrokerageOrders(IBrokerage brokerage, User user)
            {
                var orders = await brokerage.GetOrders(user.State);
                if (!orders.IsOk)
                {
                    return new Order[0];
                }

                return 
                    orders.Success
                        .Where(o => o.IncludeInResponses)
                        .OrderBy(o => o.StatusOrder)
                        .ToArray();
            }
        }
    }
}