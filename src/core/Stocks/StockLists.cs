using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using MediatR;

namespace core.Stocks
{
    public class StockLists
    {
        public class QueryMostActive : IRequest<List<StockQueryResult>>
        {
        }

        public class QueryLosers : IRequest<List<StockQueryResult>>
        {
        }

        public class QueryGainers : IRequest<List<StockQueryResult>>
        {
        }

        public class Handler :
            IRequestHandler<QueryMostActive, List<StockQueryResult>>,
            IRequestHandler<QueryLosers, List<StockQueryResult>>,
            IRequestHandler<QueryGainers, List<StockQueryResult>>
        {
            private IStocksLists _lists;

            public Handler(IStocksLists lists)
            {
                _lists = lists;
            }

            public async Task<List<StockQueryResult>> Handle(QueryMostActive request, CancellationToken cancellationToken)
            {
                return await _lists.GetMostActive();
            }

            public async Task<List<StockQueryResult>> Handle(QueryLosers request, CancellationToken cancellationToken)
            {
                return await _lists.GetLosers();
            }

            public async Task<List<StockQueryResult>> Handle(QueryGainers request, CancellationToken cancellationToken)
            {
                return await _lists.GetGainers();
            }
        }
    }
}