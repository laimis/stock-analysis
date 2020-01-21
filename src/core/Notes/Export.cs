using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Notes
{
    public class Export
    {
        public class Query : IRequest<core.ExportResponse>
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

            public override async Task<ExportResponse> Handle(Query query, CancellationToken cancellationToken)
            {
                var notes = await _storage.GetNotes(query.UserId);

                var filename = CSVExport.GenerateFilename("notes");

                return new ExportResponse(filename, CSVExport.Generate(notes));
            }
        }
    }
}