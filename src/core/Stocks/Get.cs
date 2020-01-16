using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using MediatR;

namespace core.Stocks
{
    public class Get
    {
        public class Query : IRequest<object>
        {
            public string Ticker { get; private set; }

            public Query(string ticker)
            {
                this.Ticker = ticker;
            }
        }

        public class Handler : IRequestHandler<Query, object>
        {
            private IStocksService _stocksService;

            public Handler(IStocksService stocksService)
            {
                _stocksService = stocksService;
            }

            public async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var profile = await _stocksService.GetCompanyProfile(request.Ticker);

                var data = await _stocksService.GetHistoricalDataAsync(request.Ticker);

                var metrics = await _stocksService.GetKeyMetrics(request.Ticker);
                
                return Mapper.MapStockDetail(request.Ticker, profile, data, metrics);
            }
        }
    }
}