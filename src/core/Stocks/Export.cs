using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Stocks
{
    public class Export
    {
        public class Query : IRequest<ExportResponse>
        {
            public Query(string userId)
            {
                this.UserId = userId;
            }

            public string UserId { get; }
        }

        public class Handler : HandlerWithStorage<Query, ExportResponse>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<ExportResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var options = await _storage.GetStocks(request.UserId);

                var filename = CSVExport.GenerateFilename("stocks");

                return new ExportResponse(filename, CSVExport.Generate(options));
            }
        }
    }
}