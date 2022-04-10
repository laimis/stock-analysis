using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using MediatR;

namespace core.Stocks
{
    public class Search
    {
        public class Query : IRequest<List<SearchResult>>
        {
            public string Term { get; private set; }

            public Query(string term)
            {
                Term = term;
            }
        }

        public class Handler : IRequestHandler<Query, List<SearchResult>>
        {
            private IStocksService2 _stocksService;

            public Handler(IStocksService2 stockService2)
            {
                _stocksService = stockService2;
            }

            public async Task<List<SearchResult>> Handle(Query request, CancellationToken cancellationToken) =>
                (await _stocksService.Search(request.Term, 5)).Success;
        }
    }
}