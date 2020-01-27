using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Stocks
{
    public class Export
    {
        public class Query : RequestWithUserId<ExportResponse>
        {
            public Query(string userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, ExportResponse>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<ExportResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var filename = CSVExport.GenerateFilename("stocks");

                return new ExportResponse(filename, CSVExport.Generate(stocks));
            }
        }
    }
}