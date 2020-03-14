using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Notes;
using core.Options;
using core.Portfolio.Output;
using core.Shared;
using core.Stocks;

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

                return stocks.Select(async s => {
                    {
                        var adv = await _stocks.GetAdvancedStats(s.Ticker);
                        var price = await _stocks.GetPrice(s.Ticker);

                        return new GridEntry(s.Ticker, price, adv);
                    }
                }).Select(t => t.Result);
            }
        }
    }
}