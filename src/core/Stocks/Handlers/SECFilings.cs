using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared.Adapters.SEC;
using MediatR;

namespace core.Stocks.Handlers
{
    public class SECFilings
    {
        public class Query : IRequest<CompanyFilings>
        {
            public string Ticker { get; }
            public Guid UserId { get; }

            public Query(string ticker, Guid userId)
            {
                Ticker = ticker;
                UserId = userId;
            }
        }

        public class Handler : IRequestHandler<Query, CompanyFilings>
        {
            private ISECFilings _filings;

            public Handler(ISECFilings filings)
            {
                _filings = filings;
            }

            public Task<CompanyFilings> Handle(Query request, CancellationToken cancellationToken)
                => _filings.GetFilings(request.Ticker);
        }
    }
}