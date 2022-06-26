using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared.Adapters.Stocks;
using MediatR;

namespace core.Stocks.Handlers
{
    public class Prices
    {
        public class Query : IRequest<StockServiceResponse<HistoricalPrice[]>>
        {
            public string Interval { get; }
            public string Ticker { get; }

            public Query(string ticker, string interval)
            {
                Interval = interval;
                Ticker = ticker;
            }
        }

        public class Handler : IRequestHandler<Query, StockServiceResponse<HistoricalPrice[]>>
        {
            private IStocksService2 _stocksService2;

            public Handler(IStocksService2 stockService2)
            {
                _stocksService2 = stockService2;
            }

            public Task<StockServiceResponse<HistoricalPrice[]>> Handle(Query request, CancellationToken cancellationToken)
            {
                return _stocksService2.GetHistoricalPrices(request.Ticker, request.Interval);
            }
        }
    }
}