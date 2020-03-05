using System;
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
            [Required]
            public string Ticker { get; set; }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {

            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var options = await _storage.GetOwnedOptions(request.UserId);

                var filtered = options.Where(o => o.IsActive && o.Ticker == request.Ticker);
                
                return Mapper.ToOptions(filtered);
            }
        }
    }
}