using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using MediatR;

namespace core.Stocks
{
    public class Search
    {
        public class Query : IRequest<object>
        {
            public string Term { get; private set; }

            public Query(string term)
            {
                this.Term = term;
            }
        }

        public class Handler : IRequestHandler<Query, object>
        {
            private IStocksService2 _stocksService;

            public Handler(IStocksService2 stockService2)
            {
                _stocksService = stockService2;
            }

            public async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var list = await _stocksService.Search(request.Term);

                return list
                    .Where(s => s.IsCommonShare && s.Region == "US")
                    .Take(5);
            }
        }
    }
}