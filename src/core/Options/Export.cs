using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Options
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
                var options = await _storage.GetSoldOptions(request.UserId);

                var filename = CSVExport.GenerateFilename("options");

                return new ExportResponse(filename, CSVExport.Generate(options));
            }
        }
    }
}