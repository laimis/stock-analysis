using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;

namespace core.Portfolio.Handlers
{
    public class PendingStockPositionsGet
    {
        public class Query : RequestWithUserId<IEnumerable<PendingStockPositionState>>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, IEnumerable<PendingStockPositionState>>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;

            public Handler(
                IAccountStorage accounts,
                IBrokerage brokerage,
                IPortfolioStorage storage
                ) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }

            public override async Task<IEnumerable<PendingStockPositionState>> Handle(Query query, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(query.UserId);
                if (user == null)
                {
                    throw new UnauthorizedAccessException("Unable to find user");
                }
                
                var positions = await _storage.GetPendingStockPositions(query.UserId);

                // get prices for each position
                var tickers = positions.Select(x => x.State.Ticker).Distinct();

                var prices = await _brokerage.GetQuotes(user.State, tickers);

                foreach (var p in positions)
                {
                    if (prices.Success.TryGetValue(p.State.Ticker, out var quote))
                    {
                        p.SetPrice(quote.Price);
                    }
                }

                return positions
                    .OrderByDescending(x => x.State.Date)
                    .Select(x => x.State);
            }
        }
    }
}