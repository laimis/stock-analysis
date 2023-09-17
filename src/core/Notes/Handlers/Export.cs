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
        public class Query : RequestWithUserId<ServiceResponse<ExportResponse>>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, ServiceResponse<ExportResponse>>
        {
            private ICSVWriter _csvWriter;

            public Handler(
                ICSVWriter csvWriter,
                IPortfolioStorage storage) : base(storage)
            {
                _csvWriter = csvWriter;
            }

            public override async Task<ServiceResponse<ExportResponse>> Handle(Query query, CancellationToken cancellationToken)
            {
                var notes = await _storage.GetNotes(query.UserId);

                var filename = CSVExport.GenerateFilename("notes");

                var response = new ExportResponse(filename, CSVExport.Generate(_csvWriter, notes));
                
                return new ServiceResponse<ExportResponse>(response);
            }
        }
    }
}