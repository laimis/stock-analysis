using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Alerts.Handlers
{
    public class Monitors
    {
        
        public const string PATTERN_TAG = "monitor:patterns";
        public const string PATTERN_TAG_NAME = "Patterns";
        public record struct MonitorDescriptor(string name, string tag);

        public class Query : RequestWithUserId<MonitorDescriptor[]>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : IRequestHandler<Query, MonitorDescriptor[]>
        {
            public Task<MonitorDescriptor[]> Handle(Query request, CancellationToken cancellationToken)
            {
                var monitors = new [] {
                    new MonitorDescriptor(PATTERN_TAG_NAME, PATTERN_TAG)
                };

                return Task.FromResult(monitors);
            }
        }
    }
}