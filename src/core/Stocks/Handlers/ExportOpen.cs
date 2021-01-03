using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Stocks.View;

namespace core.Stocks
{
    public class ExportOpen
    {
        public class Query : RequestWithUserId<ExportResponse>
        {
            public Query(Guid userId) : base(userId)
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

                var open = stocks
                    .Where(s => s.State.Owned > 0)
                    .Select(s => new OwnedStockView(s))
                    .OrderByDescending(s => s.Cost);

                var filename = CSVExport.GenerateFilename("openstocks");

                return new ExportResponse(filename, CSVExport.Generate(open));
            }
        }
    }
}