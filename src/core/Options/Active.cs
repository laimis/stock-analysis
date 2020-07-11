using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Options
{
    public class Active
    {
        public class Query : RequestWithUserId<OwnedOptionStatsContainer>
        {
            public Query(string ticker, Guid userId) :base(userId)
            {
                this.Ticker = ticker;
            }

            [Required]
            public string Ticker { get; set; }
        }

        public class Handler : HandlerWithStorage<Query, OwnedOptionStatsContainer>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<OwnedOptionStatsContainer> Handle(Query request, CancellationToken cancellationToken)
            {
                var options = await _storage.GetOwnedOptions(request.UserId);

                options = options.Where(o => !o.State.Deleted && o.IsActive && o.Ticker == request.Ticker);
                options = options.OrderByDescending(o => o.State.FirstFill);

                return Map(options);
            }

            private OwnedOptionStatsContainer Map(IEnumerable<OwnedOption> options)
            {
                var open = options.Select(o => Map(o));

                return new OwnedOptionStatsContainer(new List<OwnedOptionSummary>(), open);
            }

            private OwnedOptionSummary Map(OwnedOption o)
            {
                return new OwnedOptionSummary(o, new TickerPrice());
            }
        }
    }
}