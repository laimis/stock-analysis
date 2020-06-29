using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Alerts
{
    public class Get
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId, string ticker) : base(userId)
            {
                this.Ticker = ticker;
            }

            public string Ticker { get; }
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
                var alert = await _alertsStorage.GetAlert(request.Ticker, request.UserId);

                if (alert == null)
                {
                    return null;
                }

                return new {
                    ticker = alert.State.Ticker.Value,
                    points = alert.State.PricePoints
                };
            }
        }
    }
}