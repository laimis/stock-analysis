using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.CSV;
using core.Shared.Adapters.Storage;

namespace core.Notes.Handlers
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

            public override async Task<ExportResponse> Handle(Query query, CancellationToken cancellationToken)
            {
                var notes = await _storage.GetNotes(query.UserId);

                var filename = CSVExport.GenerateFilename("notes");

                return new ExportResponse(filename, CSVExport.Generate(_csvWriter, notes));
            }
        }
    }
}