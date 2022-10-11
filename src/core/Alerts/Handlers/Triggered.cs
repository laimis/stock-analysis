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
        public class Query : IRequest<List<PositionInstance>>
        {
            public Query()
            {
            }
        }

        public class Handler : IRequestHandler<Query, List<PositionInstance>>
        {
            private StockMonitorContainer _container;

            public Handler(StockMonitorContainer container)
            {
                _container = container;
            }

            public Task<List<PositionInstance>> Handle(Query request, CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    _container.Monitors.Where(x => x.IsTriggered).Select(x => x.Position).ToList()
                );
            }
        }
    }
}