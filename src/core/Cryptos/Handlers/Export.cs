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

            public override async Task<ExportResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var cryptos = await _storage.GetCryptos(request.UserId);

                var filename = CSVExport.GenerateFilename("cryptos");

                return new ExportResponse(filename, CSVExport.Generate(cryptos));
            }
        }
    }
}