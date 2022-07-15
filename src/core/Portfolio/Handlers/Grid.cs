using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Portfolio
{
    public class Grid
    {
        public class Generate : RequestWithUserId<IEnumerable<GridEntry>>
        {
        }

        public class Handler : HandlerWithStorage<Generate, IEnumerable<GridEntry>>
        {
            public Handler(
                IPortfolioStorage storage,
                IStocksService2 stocks) : base(storage)
            {
                _stocks = stocks;
            }

            private IStocksService2 _stocks { get; }

            public override async Task<IEnumerable<GridEntry>> Handle(Generate request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var prices = await _stocks.GetPrices(stocks.Select(s => s.State.Ticker).ToArray());

                return stocks
                    .Where(s => s.State.Owned > 0)
                    .Select(async s => {
                        {
                            var adv = await _stocks.GetAdvancedStats(s.State.Ticker);
                            var price = prices.Success.TryGetValue(s.State.Ticker, out var p) ? p.Price : 0;

                            return new GridEntry(s.State.Ticker, price, adv.Success);
                        }
                    })
                    .Select(t => t.Result);
            }
        }
    }
}