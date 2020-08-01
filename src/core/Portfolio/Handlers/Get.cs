using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Alerts;
using core.Shared;

namespace core.Portfolio
{
    public class Get
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            private IStocksService2 _stocksService;
            private StockMonitorContainer _alerts;

            public Handler(
                IPortfolioStorage storage,
                IStocksService2 stockService,
                StockMonitorContainer alerts) : base(storage)
            {
                _stocksService = stockService;
                _alerts = alerts;
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var owned = stocks.Where(s => s.State.Owned > 0);

                var options = await _storage.GetOwnedOptions(request.UserId);

                var openOptions = options
                    .Where(o => o.State.NumberOfContracts != 0 && o.State.DaysUntilExpiration > -5)
                    .OrderBy(o => o.State.Expiration);

                var prices = owned.Select(o => o.Ticker).Union(openOptions.Select(o => o.State.Ticker))
                    .Distinct()
                    .ToDictionary(s => s, async s => await _stocksService.GetPrice(s));

                var obj = new
                {
                    owned = owned.Select(o => Mapper.ToOwnedView(o, prices[o.Ticker].Result)),
                    openOptions = openOptions.Select(o => new Options.OwnedOptionSummary(o, prices[o.State.Ticker].Result)),
                    triggered = _alerts.Monitors.Where(s => s.Alert.UserId == request.UserId && s.IsTriggered),
                    alerts = _alerts.Monitors.Where(s => s.Alert.UserId == request.UserId)
                };

                return obj;
            }
        }
    }
}