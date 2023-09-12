using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Storage;

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
            private readonly IAccountStorage _accounts;
            private readonly IBrokerage _brokerage;

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
                var user = await _accounts.GetUser(query.UserId)
                    ?? throw new UnauthorizedAccessException("Unable to find user");

                var positions = await _storage.GetPendingStockPositions(query.UserId);

                // get prices for each position
                var tickers = positions.Select(x => x.State.Ticker).Distinct();

                var prices = await _brokerage.GetQuotes(user.State, tickers);
                var pricesDict = prices.IsOk ? prices.Success : new Dictionary<string, StockQuote>();

                return positions
                    .Where(x => !x.State.IsClosed)
                    .OrderByDescending(x => x.State.Date)
                    .Select(x => {
                        var state = x.State;
                        state.SetPrice(pricesDict.GetValueOrDefault(state.Ticker)?.Price ?? 0);
                        return state;
                    });
            }
        }
    }
}