using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Stocks
{
    public class Dashboard
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

                var ownedStocks = stocks.Where(s => s.State.Owned > 0).ToList();
                
                var closedTransactions = stocks.Where(s => s.State.Owned == 0)
                    .SelectMany(s => s.State.Transactions.Where(t => t.IsPL))
                    .ToList();

                var owned = MapOwnedStocks(ownedStocks);

                var performance = new StockOwnershipPerformance(closedTransactions);

                var obj = new
                {
                    owned,
                    performance
                };

                return obj;
            }

            private IEnumerable<object> MapOwnedStocks(IEnumerable<OwnedStock> stocks)
            {
                var prices = stocks
                    .Select(o => o.State.Ticker).Distinct()
                    .ToDictionary(s => s, async s => await _stocksService.GetPrice(s));

                return stocks.Select(o => Mapper.ToOwnedView(o, prices[o.State.Ticker].Result));
            }
        }
    }
}