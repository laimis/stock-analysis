using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

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
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<ExportResponse> Handle(Query request, CancellationToken cancellationToken) =>
                new ExportResponse(
                    CSVExport.GenerateFilename("cryptos"),
                    CSVExport.Generate(
                        (await _storage.GetCryptos(request.UserId))
                    )
                );
        }
    }
}