using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Alerts
{
    public class Monitors
    {
        public class Query : IRequest<List<IStockPositionMonitor>>
        {
            public Query()
            {
            }
        }

        public class Handler : IRequestHandler<Query, List<IStockPositionMonitor>>
        {
            private StockMonitorContainer _container;

            public Handler(StockMonitorContainer container)
            {
                _container = container;
            }

            public Task<List<IStockPositionMonitor>> Handle(Query request, CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    _container.Monitors
                        .OrderByDescending(m => m.IsTriggered)
                        .ThenBy(p => p.Ticker)
                        .ThenBy(p => p.Description)
                        .ToList()
                );
            }
        }
    }
}