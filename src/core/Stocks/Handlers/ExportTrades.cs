using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.CSV;
using core.Stocks.View;

namespace core.Stocks
{
    public class ExportTrades
    {
        public class Query : RequestWithUserId<ExportResponse>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, ExportResponse>
        {
            private ICSVWriter _csvWriter;

            public Handler(
                ICSVWriter csvWriter,
                IPortfolioStorage storage) : base(storage)
            {
                _csvWriter = csvWriter;
            }

            public override async Task<ExportResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var trades = stocks
                    .SelectMany(s => s.State.PositionInstances)
                    .Where(p => p.IsClosed)
                    .OrderByDescending(p => p.Closed)
                    .ToList();

                var filename = CSVExport.GenerateFilename("closedstocks");

                return new ExportResponse(filename, CSVExport.Generate(_csvWriter, trades));
            }
        }
    }
}