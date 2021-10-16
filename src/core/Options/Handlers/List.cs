using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Options
{
    public class List
    {
        public class Query : RequestWithUserId<OwnedOptionStatsView>
        {
            public Query(string ticker, Guid userId) :base(userId)
            {
                Ticker = ticker;
            }

            [Required]
            public string Ticker { get; set; }
        }

        public class Handler : HandlerWithStorage<Query, OwnedOptionStatsView>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<OwnedOptionStatsView> Handle(Query request, CancellationToken cancellationToken)
            {
                var options = await _storage.GetOwnedOptions(request.UserId);

                var open = options
                    .Where(o => o.State.Active && o.State.Ticker == request.Ticker)
                    .OrderByDescending(o => o.State.FirstFill)
                    .Select(o => new OwnedOptionView(o));

                return new OwnedOptionStatsView(open);
            }
        }
    }
}