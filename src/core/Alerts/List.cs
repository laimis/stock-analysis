using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
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
            private IStocksService2 _stock;
            private IAlertsStorage _alertsStorage;
            private StockMonitorContainer _container;

            public Handler(
                IPortfolioStorage storage,
                IAlertsStorage alertsStorage,
                IStocksService2 stockService,
                StockMonitorContainer container) : base(storage)
            {
                _stock = stockService;
                _alertsStorage = alertsStorage;
                _container = container;
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var alerts = await _alertsStorage.GetAlerts(request.UserId);

                var list = new List<object>();

                foreach(var a in alerts.OrderBy(a => a.Ticker))
                {
                    if (a.PricePoints.Count == 0)
                    {
                        continue;
                    }
                    
                    var price = await _stock.GetPrice(a.Ticker);

                    list.Add(new {
                        currentPrice = price.Amount,
                        ticker = a.State.Ticker.Value,
                        points = a.State.PricePoints.Select(pp => new {
                            pp.Id,
                            pp.Value,
                            pp.Description,
                            triggered = _container.HasTriggered(pp)
                        }),
                    });
                }

                return list;
            }
        }
    }
}