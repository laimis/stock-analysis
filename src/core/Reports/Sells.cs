using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Stocks;
using core.Stocks.View;

namespace core.Reports
{
    public class Sells
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var sells = stocks
                    .SelectMany(s => s.State.BuyOrSell.Select(t => new { stock = s, buyOrSell = t}))
                    .Where(s => s.buyOrSell is StockSold)
                    .Where(s => s.buyOrSell.When > DateTimeOffset.UtcNow.AddDays(-60))
                    .GroupBy(s => s.stock.State.Ticker)
                    .Select(g => new {ticker = g.Key, latest = g.OrderByDescending(s => s.buyOrSell.When).First()})
                    .Select(t => new 
                    {
                        ticker = t.ticker,
                        date = t.latest.buyOrSell.When,
                        numberOfShares = t.latest.buyOrSell.NumberOfShares,
                        price = t.latest.buyOrSell.Price,
                        olderThan30Days = t.latest.buyOrSell.When < DateTimeOffset.UtcNow.AddDays(-30)
                    })
                    .OrderByDescending(a => a.date)
                    .ToList();

                return sells;
            }
        }
    }
}