using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Portfolio.Views;
using core.Shared.Adapters.Brokerage;
using core.Stocks;
using core.Stocks.Services.Trading;
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

                var account = await GetAccount(user);

                var positions = stocks.Where(s => s.State.OpenPosition != null)
                    .Select(s => s.State.OpenPosition)
                    .ToArray();

                var tickers = positions.Select(p => p.Ticker).Union(account.StockPositions.Select(p => p.Ticker)).Distinct();

                var prices = (await _brokerage.GetQuotes(user.State, tickers)).Success ?? new Dictionary<string, StockQuote>(); 
                foreach (var entry in positions)
                {
                    prices.TryGetValue(entry.Ticker, out var price);
                    entry.SetPrice(price?.Price ?? 0);    
                }

                var current = positions
                    .OrderByDescending(s => s.RR)
                    .ToArray();

                var past = stocks
                    .SelectMany(s => s.State.GetClosedPositions())
                    .OrderByDescending(p => p.Closed)
                    .ToArray();

                var performance = new TradingPerformanceContainerView(
                    new Span<PositionInstance>(past),
                    20
                );

                var strategyPerformance = past.Where(p => p.ContainsLabel(key: "strategy"))
                    .GroupBy(p => p.GetLabelValue(key: "strategy"))
                    .Select(g => {
                        var trades = g.ToArray(); 
                        return new TradingStrategyPerformance(
                            strategyName: g.Key,
                            performance: TradingPerformance.Create(trades),
                            positions: trades
                        );
                    })
                    .OrderByDescending(p => p.performance.Profit)
                    .ToArray();

            
                var violations = Dashboard.Handler.GetViolations(account.StockPositions, positions, prices);

                return new TradingEntriesView(
                    current: current,
                    past: past,
                    performance: performance,
                    violations: violations,
                    strategyPerformance: strategyPerformance,
                    cashBalance: account.CashBalance,
                    brokerageOrders: account.Orders
                );
            }

            private async Task<TradingAccount> GetAccount(User user)
            {
                TradingAccount EmptyAccount() => new TradingAccount {
                        StockPositions = new StockPosition[0],
                        Orders = new Order[0]
                    };

                if (user.State.ConnectedToBrokerage == false)
                {
                    return EmptyAccount();
                }

                var account = await _brokerage.GetAccount(user.State);

                return account.IsOk switch {
                    true => account.Success,
                    false => EmptyAccount()
                };
            }
        }
    }
}