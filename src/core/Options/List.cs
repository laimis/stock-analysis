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
        public class Query : RequestWithUserId<object>
        {
            public Query(string ticker, bool? activeFilter, Guid userId) :base(userId)
            {
                this.Ticker = ticker;
                this.ActiveFilter = activeFilter;
            }

            [Required]
            public string Ticker { get; set; }
            public bool? ActiveFilter { get; }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var options = await _storage.GetOwnedOptions(request.UserId);

                options = options.Where(o => !o.State.Deleted);

                if (request.ActiveFilter.HasValue)
                {
                    options = options.Where(o => o.IsActive == request.ActiveFilter.Value);
                }

                if (request.Ticker != null)
                {
                    options = options.Where(o => o.Ticker == request.Ticker);
                }

                options = options.OrderByDescending(o => o.State.FirstFill);

                return Map(options);
            }

            private object Map(IEnumerable<OwnedOption> options)
            {
                var summaries = options.Select(o => Map(o));

                var overall = new OwnedOptionStats(summaries);
                var buy = new OwnedOptionStats(summaries.Where(s => s.BoughtOrSold == "Bought"));
                var sell = new OwnedOptionStats(summaries.Where(s => s.BoughtOrSold == "Sold"));

                return new {
                    overall,
                    buy,
                    sell,
                    options = summaries
                };
            }

            private OwnedOptionSummary Map(OwnedOption o)
            {
                return new OwnedOptionSummary(o, new TickerPrice());
            }
        }
    }
}