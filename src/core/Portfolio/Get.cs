using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Portfolio
{
    public class Get
    {
        public class Query : IRequest<object>
        {
            public Query(string userId)
            {
                this.UserId = userId;
            }

            public string UserId { get; }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var cashedout = stocks.Where(s => s.State.Owned == 0);
                var owned = stocks.Where(s => s.State.Owned > 0);

                var totalSpent = stocks.Sum(s => s.State.Spent);
                var totalEarned = stocks.Sum(s => s.State.Earned);

                var options = await _storage.GetSoldOptions(request.UserId);

                var ownedOptions = options.Where(o => o.State.Amount > 0);
                var closedOptions = options.Where(o => o.State.Amount == 0);

                var obj = new
                {
                    totalSpent,
                    totalEarned,
                    totalCashedOutSpend = cashedout.Sum(s => s.State.Spent),
                    totalCashedOutEarnings = cashedout.Sum(s => s.State.Earned),
                    owned = owned.Select(o => Mapper.ToOwnedView(o)),
                    cashedOut = cashedout.Select(o => Mapper.ToOwnedView(o)),
                    ownedOptions = ownedOptions.Select(o => Mapper.ToOptionView(o)),
                    closedOptions = closedOptions.Select(o => Mapper.ToOptionView(o)),
                    pendingPremium = ownedOptions.Sum(o => o.State.Premium),
                    collateralCash = ownedOptions.Sum(o => o.State.CollateralCash),
                    collateralShares = ownedOptions.Sum(o => o.State.CollateralShares),
                    optionEarnings = options.Sum(o => o.State.Profit)
                };

                return obj;
            }
        }
    }
}