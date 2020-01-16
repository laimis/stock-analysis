using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using MediatR;

namespace core.Stocks
{
    public class MostActive
    {
        public class Query : IRequest<List<MostActiveEntry>>
        {
        }

        public class Handler : IRequestHandler<Query, List<MostActiveEntry>>
        {
            private IStocksLists _lists;

            public Handler(IStocksLists lists)
            {
                _lists = lists;
            }

            public async Task<List<MostActiveEntry>> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _lists.GetMostActive();
            }
        }
    }
}