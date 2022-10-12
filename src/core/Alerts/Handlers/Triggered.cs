using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Stocks;
using MediatR;

namespace core.Alerts
{
    public class Triggered
    {
        public class Query : IRequest<List<StockMonitorTrigger>>
        {
            public Query()
            {
            }
        }

        public class Handler : IRequestHandler<Query, List<StockMonitorTrigger>>
        {
            private StockMonitorContainer _container;

            public Handler(StockMonitorContainer container)
            {
                _container = container;
            }

            public Task<List<StockMonitorTrigger>> Handle(Query request, CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    _container.Monitors
                        .Where(x => x.IsTriggered)
                        .Select(x => x.Trigger.Value).ToList()
                );
            }
        }
    }
}