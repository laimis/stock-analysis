using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Reports.Views;
using core.Shared;

namespace core.Reports
{
    public class Sells
    {
        public class Query : RequestWithUserId<SellsView>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, SellsView>
        {
            private IStocksService2 _stockService;

            public Handler(IPortfolioStorage storage, IStocksService2 stocksService) : base(storage) =>
                _stockService = stocksService;

            public override async Task<SellsView> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var prices = await _stockService.GetPrices(stocks.Select(s => s.State.Ticker));

                return new SellsView(stocks, prices);
            }
        }
    }
}