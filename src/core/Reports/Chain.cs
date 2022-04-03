using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Stocks.View;

namespace core.Reports
{
    public class Chain
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

            public override Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                return RealThing(request);
            }

            private async Task<object> RealThing(Query request)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var links = stocks
                    .SelectMany(s => s.State.Transactions.Where(t => t.IsPL))
                    .Select(t => new StockTransactionView(t))
                    .OrderByDescending(p => p.Date)
                    .Select(t => new
                    {
                        success = t.Profit >= 0,
                        ticker = t.Ticker,
                        level = GetLevel(t)
                    })
                    .ToList();

                return new { links };
            }

            private int GetLevel(StockTransactionView t) =>
                Math.Abs(t.Profit) switch
                {
                    var p when p > 1000 => 3,
                    var p when p > 500 => 2,
                    _ => 1
                };
        }
    }
}