using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Alerts
{
    public class List
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            private IAlertsStorage _alertsStorage;

            public Handler(
                IPortfolioStorage storage,
                IAlertsStorage alertsStorage) : base(storage)
            {
                _alertsStorage = alertsStorage;
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var alerts = await _alertsStorage.GetAlerts(request.UserId);

                return alerts.Select(a => new {
                    ticker = a.State.Ticker.Value,
                    a.State.Threshold
                });
            }
        }
    }
}