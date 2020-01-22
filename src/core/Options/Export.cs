using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Options
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
                var options = await _storage.GetSoldOptions(request.UserId);

                var filename = CSVExport.GenerateFilename("options");

                return new ExportResponse(filename, CSVExport.Generate(options));
            }
        }
    }
}