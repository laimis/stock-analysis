using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.CSV;
using core.Shared.Adapters.Storage;

namespace core.Stocks.Handlers
{
    public class ExportTransactions
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

            public override async Task<ServiceResponse<ExportResponse>> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var filename = CSVExport.GenerateFilename("stocks");

                var response = new ExportResponse(filename, CSVExport.Generate(_csvWriter, stocks));
                
                return new ServiceResponse<ExportResponse>(response);
            }
        }
    }
}