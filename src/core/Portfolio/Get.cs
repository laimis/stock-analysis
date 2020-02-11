using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var owned = stocks.Where(s => s.State.Owned > 0);

                var options = await _storage.GetOwnedOptions(request.UserId);

                var openOptions = options
                    .Where(o => o.State.NumberOfContracts != 0 && o.State.ExpiredDays > -5)
                    .OrderBy(o => o.State.Expiration);

                var obj = new
                {
                    owned = owned.Select(o => Mapper.ToOwnedView(o)),
                    openOptions = openOptions.Select(o => Mapper.ToOptionView(o))
                };

                return obj;
            }
        }
    }
}