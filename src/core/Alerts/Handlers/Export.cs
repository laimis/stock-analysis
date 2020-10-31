using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Stocks.View;

namespace core.Alerts
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
            private IAlertsStorage _alertsStorage;

            public Handler(
                IAlertsStorage alertsStorage,
                IPortfolioStorage storage
                ) : base(storage)
            {
                _alertsStorage = alertsStorage;
            }

            public override async Task<ExportResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var alerts = await _alertsStorage.GetAlerts(request.UserId);

                var filename = CSVExport.GenerateFilename("alerts");

                return new ExportResponse(filename, CSVExport.Generate(alerts));
            }
        }
    }
}