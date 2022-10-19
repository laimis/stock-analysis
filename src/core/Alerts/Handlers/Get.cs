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
            private StockMonitorContainer _container;

            public Handler(StockMonitorContainer container) => _container = container;

            public Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var monitors = 
                    _container.GetMonitors(request.UserId)
                        .OrderByDescending(m => m.IsTriggered)
                        .ThenBy(p => p.Ticker)
                        .ThenBy(p => p.Description)
                        .ToList();

                var recentlyTriggered = _container.GetRecentlyTriggeredAlerts(request.UserId);

                return Task.FromResult<object>(
                    new {
                        monitors,
                        recentlyTriggered
                    }
                );
            }
        }
    }
}