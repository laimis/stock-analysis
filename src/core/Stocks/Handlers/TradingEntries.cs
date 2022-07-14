using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using MediatR;

namespace core.Stocks.Views
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
            private IPortfolioStorage _portfolio;
            private IStocksService2 _stocks;

            public Handler(IPortfolioStorage portfolio, IStocksService2 stocks)
            {
                _portfolio = portfolio;
                _stocks = stocks;
            }

            public async Task<TradingEntriesView> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _portfolio.GetStocks(request.UserId);

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
                    .ToArray();

                var past = closedPositions
                    .OrderByDescending(p => p.Closed)
                    .ToArray();

                return new TradingEntriesView(
                    current: current,
                    past: past
                );
                
            }
        }
    }
}