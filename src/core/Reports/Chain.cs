using System;
using System.Collections.Generic;
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

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                return await RealThing(request);
            }

            // public Task<object> Random()
            // {
            //     var links = new List<object>();

            //     var rand = new Random();

            //     const int count = 241;

            //     while (links.Count < count)
            //     {
            //         links.Add(new { success = rand.NextDouble() >= 0.5 });
            //     }

            //     return Task.FromResult(new { links });
            // }

            private async Task<object> RealThing(Query request)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var links = stocks
                    .SelectMany(s => s.State.Transactions.Where(t => t.IsPL))
                    .Select(t => new StockTransactionView(t))
                    .OrderByDescending(p => p.Date)
                    .Select(t => new
                    {
                        success = t.Profit >= 0
                    })
                    .ToList();

                return new { links };
            }
        }
    }
}