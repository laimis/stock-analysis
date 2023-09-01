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
        public class Query : RequestWithUserId<CommandResponse<IEnumerable<OwnedOptionView>>>
        {
            public Query(Ticker ticker, Guid userId) :base(userId)
            {
                Ticker = ticker;
            }

            [Required]
            public Ticker Ticker { get; set; }
        }

        public class Handler : HandlerWithStorage<Query, CommandResponse<IEnumerable<OwnedOptionView>>>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<CommandResponse<IEnumerable<OwnedOptionView>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var options = await _storage.GetOwnedOptions(request.UserId);

                var open = options
                    .Where(o => o.State.Active && o.State.Ticker == request.Ticker)
                    .OrderByDescending(o => o.State.FirstFill)
                    .Select(o => new OwnedOptionView(o.State, optionDetail: null));

                return CommandResponse<IEnumerable<OwnedOptionView>>.Success(open);
            }
        }
    }
}