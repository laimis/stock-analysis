using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared;
using MediatR;

namespace core.Stocks.Handlers
{
    public class Price
    {
        public class Query : IRequest<decimal>
        {
            public Ticker Ticker { get; }

            public Query(string ticker)
            {
                Ticker = ticker;
            }
        }

        public class Handler : IRequestHandler<Query, decimal>
        {
            private IStocksService2 _stocksService2;

            public Handler(IStocksService2 stockService2)
            {
                _stocksService2 = stockService2;
            }

            public async Task<decimal> Handle(Query request, CancellationToken cancellationToken)
            {
                var price = await _stocksService2.GetPrice(request.Ticker);

                return price.Success.Amount;
            }
        }
    }
}