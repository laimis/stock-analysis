using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.CSV;

namespace core.Stocks
{
    public class ExportTrades
    {
        public enum ExportType
        {
            Open,
            Closed
        }

        public class Query : RequestWithUserId<ExportResponse>
        {
            public Query(Guid userId, ExportType exportType) : base(userId)
            {
                ExportType = exportType;
            }

            public ExportType ExportType { get; }
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

                var trades = request.ExportType switch {
                    ExportType.Open => stocks.Where(s => s.State.OpenPosition != null).Select(s => s.State.OpenPosition),
                    ExportType.Closed => stocks.SelectMany(s => s.State.ClosedPositions),
                    _ => throw new NotImplementedException()
                };

                var final = trades
                    .OrderByDescending(p => p.Closed ?? p.Opened)
                    .ToList();

                var filename = CSVExport.GenerateFilename("positions");

                return new ExportResponse(filename, CSVExport.Generate(_csvWriter, trades));
            }
        }
    }
}