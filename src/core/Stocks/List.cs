using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Stocks
{
    public class List
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            private IStocksService2 _stocksService;

            public Handler(IPortfolioStorage storage, IStocksService2 stockService) : base(storage)
            {
                _stocksService = stockService;
            }

            public override async Task<object> Handle(Query query, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(query.UserId);

                stocks = stocks.Where(s => s.State.Owned > 0);

                var prices = stocks
                    .Select(o => o.Ticker).Distinct()
                    .ToDictionary(s => s, async s => await _stocksService.GetPrice(s));

                var obj = new
                {
                    owned = stocks.Select(o => Mapper.ToOwnedView(o, prices[o.Ticker].Result)),
                };

                return obj;
            }
        }
    }
}