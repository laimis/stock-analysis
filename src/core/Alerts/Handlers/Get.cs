using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Alerts
{
    public class Get
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : IRequestHandler<Query, object>
        {
            private StockAlertContainer _container;

            public Handler(StockAlertContainer container) => _container = container;

            public Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var alerts = 
                    _container.GetAlerts(request.UserId)
                        .OrderBy(p => p.ticker)
                        .ThenBy(p => p.description)
                        .ToList();

                var recentlyTriggered = _container.GetRecentlyTriggeredAlerts(request.UserId);

                return Task.FromResult<object>(
                    new {
                        alerts,
                        recentlyTriggered,
                        messages = _container.GetMessages()
                    }
                );
            }
        }
    }
}