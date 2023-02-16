using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Portfolio.Views;
using core.Shared.Adapters.Brokerage;
using core.Stocks;
using MediatR;

namespace core.Portfolio.Handlers
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

            public Handler(
                IAccountStorage accounts,
                IBrokerage brokerage,
                IPortfolioStorage portfolio)
            {
                _accounts = accounts;
                _brokerage = brokerage;
                _portfolio = portfolio;
            }

            public async Task<TradingEntriesView> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _portfolio.GetStocks(request.UserId);

                var user = await _accounts.GetUser(request.UserId);

                var brokeragePositions = await (user.State.ConnectedToBrokerage switch {
                    true => GetBrokeragePositions(_brokerage, user),
                    false => Task.FromResult<IEnumerable<Position>>(new Position[0])
                });

                var positions = stocks.Where(s => s.State.OpenPosition != null)
                    .Select(s => s.State.OpenPosition)
                    .ToArray();

                var prices = await _brokerage.GetQuotes(user.State, positions.Select(p => p.Ticker)); 
                if (prices.IsOk)
                {
                    foreach (var entry in positions)
                    {
                        prices.Success.TryGetValue(entry.Ticker, out var price);
                        entry.SetPrice(price?.lastPrice ?? 0);    
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
        }
    }
}