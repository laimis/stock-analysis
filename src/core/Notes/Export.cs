using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Notes
{
    public class Export
    {
        public class Query : RequestWithUserId<core.ExportResponse>
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

            public override async Task<ExportResponse> Handle(Query query, CancellationToken cancellationToken)
            {
                var notes = await _storage.GetNotes(query.UserId);

                var filename = CSVExport.GenerateFilename("notes");

                return new ExportResponse(filename, CSVExport.Generate(notes));
            }
        }
    }
}