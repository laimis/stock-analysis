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

        public class Query2 : IRequest<object>
        {
            public string Ticker { get; private set; }

            public Query2(string ticker)
            {
                this.Ticker = ticker;
            }
        }

        public class Handler : 
            IRequestHandler<Query, object>,
            IRequestHandler<Query2, object>
        {
            private IStocksService _stocksService;
            private IStocksService2 _stocksService2;

            public Handler(
                IStocksService stocksService,
                IStocksService2 stockService2)
            {
                _stocksService = stocksService;
                _stocksService2 = stockService2;
            }

            public async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var profile = await _stocksService.GetCompanyProfile(request.Ticker);

                var data = await _stocksService.GetHistoricalDataAsync(request.Ticker);

                var metrics = await _stocksService.GetKeyMetrics(request.Ticker);
                
                return Mapper.MapStockDetail(request.Ticker, profile, data, metrics);
            }

            public async Task<object> Handle(Query2 request, CancellationToken cancellationToken)
            {
                var profile = await _stocksService2.GetCompanyProfile(request.Ticker);
                var advanced = await _stocksService2.GetAdvancedStats(request.Ticker);
                var price = await _stocksService2.GetPrice(request.Ticker);

                var data = await _stocksService.GetHistoricalDataAsync(request.Ticker);

                var metrics = await _stocksService.GetKeyMetrics(request.Ticker);
                
                return Mapper.MapStockDetail(request.Ticker, price.Amount, profile, advanced, data, metrics);
            }
        }
    }
}