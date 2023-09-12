using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.CSV;
using core.Shared.Adapters.Storage;

namespace core.Cryptos.Handlers
{
    public class Export
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

            public override async Task<ExportResponse> Handle(Query request, CancellationToken cancellationToken) =>
                new ExportResponse(
                    CSVExport.GenerateFilename("cryptos"),
                    CSVExport.Generate(
                        _csvWriter,
                        (await _storage.GetCryptos(request.UserId))
                    )
                );
        }
    }
}