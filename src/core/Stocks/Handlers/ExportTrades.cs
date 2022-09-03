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

                Func<PositionInstance, bool> filter = request.ExportType switch {
                    ExportType.Open => t => !t.IsClosed,
                    ExportType.Closed => t => t.IsClosed,
                    _ => throw new NotImplementedException()
                };

                var trades = stocks
                    .SelectMany(s => s.State.PositionInstances)
                    .Where(filter)
                    .OrderByDescending(p => p.Closed)
                    .ToList();

                var filename = CSVExport.GenerateFilename("positions");

                return new ExportResponse(filename, CSVExport.Generate(_csvWriter, trades));
            }
        }
    }
}