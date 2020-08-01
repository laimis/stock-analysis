using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Options
{
    public class Dashboard
    {
        public class Query : RequestWithUserId<OwnedOptionStatsContainer>
        {
            public Query(Guid userId) :base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, OwnedOptionStatsContainer>
        {
            private IStocksService2 _stockService;

            public Handler(
                IPortfolioStorage storage,
                IStocksService2 stockService) : base(storage)
            {
                _stockService = stockService;
            }

            public override async Task<OwnedOptionStatsContainer> Handle(Query request, CancellationToken cancellationToken)
            {
                var options = await _storage.GetOwnedOptions(request.UserId);
                options = options.Where(o => !o.State.Deleted);

                var openOptions = options
                    .Where(o => o.State.NumberOfContracts != 0 && o.State.DaysUntilExpiration > -5)
                    .OrderBy(o => o.State.Expiration)
                    .ToList();
                
                var prices = openOptions.Select(o => o.State.Ticker)
                    .Distinct()
                    .ToDictionary(s => s, async s => await _stockService.GetPrice(s));

                var closedOptions = options
                    .Where(o => o.State.Closed != null)
                    .OrderByDescending(o => o.State.FirstFill);

                return Map(closedOptions, openOptions, prices);
            }

            private OwnedOptionStatsContainer Map(
                IEnumerable<OwnedOption> closed,
                List<OwnedOption> open,
                Dictionary<string, Task<TickerPrice>> prices)
            {
                return new OwnedOptionStatsContainer(
                    closed.Select(o => Map(o)),
                    open.Select(o => new Options.OwnedOptionSummary(o, prices[o.State.Ticker].Result))
                );
            }

            private OwnedOptionSummary Map(OwnedOption o)
            {
                return new OwnedOptionSummary(o, new TickerPrice());
            }
        }
    }
}