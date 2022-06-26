using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared.Adapters.Stocks;
using core.Stocks.View;
using MediatR;

namespace core.Stocks.Handlers
{
    public class Prices
    {
        public class Query : IRequest<PricesView>
        {
            public string Interval { get; }
            public string Ticker { get; }

            public Query(string ticker, string interval)
            {
                Interval = interval;
                Ticker = ticker;
            }
        }

        public class Handler : IRequestHandler<Query, PricesView>
        {
            private IStocksService2 _stocksService2;

            public Handler(IStocksService2 stockService2)
            {
                _stocksService2 = stockService2;
            }

            public async Task<PricesView> Handle(Query request, CancellationToken cancellationToken)
            {
                var prices = await _stocksService2.GetHistoricalPrices(request.Ticker, request.Interval);

                return new PricesView(
                    prices.Success,
                    new [] {20, 50, 150}
                );
            }
        }
    }
}