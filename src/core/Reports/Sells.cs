using System;
using System.Threading;
using System.Threading.Tasks;
using core.Reports.Views;
using core.Shared;

namespace core.Reports
{
    public class Sells
    {
        public class Query : RequestWithUserId<SellsView>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, SellsView>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<SellsView> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                return new SellsView(stocks);
            }
        }
    }
}