using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.CSV;

namespace core.Portfolio.Handlers
{
    public class PendingStockPositionsExport
    {
        public class Query : RequestWithUserId<ExportResponse>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, ExportResponse>
        {
            private IAccountStorage _accounts;
            private ICSVWriter _csvWriter;
            
            public Handler(
                IAccountStorage accounts,
                ICSVWriter csvWriter,
                IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
                _csvWriter = csvWriter;
            }

            public override async Task<ExportResponse> Handle(Query query, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(query.UserId)
                    ?? throw new UnauthorizedAccessException("Unable to find user");

                var positions = await _storage.GetPendingStockPositions(query.UserId);

                var data = positions.OrderByDescending(x => x.State.Date);
                
                var filename = CSVExport.GenerateFilename("pendingpositions");

                return new ExportResponse(filename, CSVExport.Generate(_csvWriter, data));
            }
        }
    }
}