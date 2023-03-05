using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Alerts.Services;
using core.Shared;
using MediatR;
using static core.Alerts.Services.Monitors;

namespace core.Alerts
{
    public class Monitors
    {
        public class Query : RequestWithUserId<IEnumerable<MonitorDescriptor>>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : IRequestHandler<Query, IEnumerable<MonitorDescriptor>>
        {
            public Task<IEnumerable<MonitorDescriptor>> Handle(Query request, CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    Services.Monitors.GetMonitors()
                );
            }
        }
    }
}