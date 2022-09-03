using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared;
using core.Stocks;

namespace core.Portfolio
{
    public class Positions
    {
        public class Query : RequestWithUserId<IEnumerable<PositionInstance>>
        {
            public Query(Guid userId) : base(userId){}
        }

        public class Handler : HandlerWithStorage<Query, IEnumerable<PositionInstance>>
        {
            public Handler(
                IPortfolioStorage storage,
                IStocksService2 stocks) : base(storage)
            {
                _stocks = stocks;
            }

            private IStocksService2 _stocks { get; }

            public override async Task<IEnumerable<PositionInstance>> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                return stocks.SelectMany(s => s.State.PositionInstances)
                    .OrderByDescending(p => p.Closed ?? p.Opened);
            }
        }
    }
}