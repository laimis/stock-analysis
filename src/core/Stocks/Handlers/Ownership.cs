using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Storage;
using core.Stocks.View;

namespace core.Stocks.Handlers
{
    public class Ownership
    {
        public class Query : RequestWithUserId<StockOwnershipView>
        {
            public Query(string ticker, Guid userId) : base(userId)
            {
                Ticker = ticker;
            }

            public string Ticker { get; }
        }

        public class Handler : HandlerWithStorage<Query, StockOwnershipView>
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

            public override async Task<StockOwnershipView> Handle(Query query, CancellationToken cancellationToken)
            {
                var stock = await _storage.GetStock(query.Ticker, query.UserId);
                if (stock == null)
                {
                    return null;
                }

                var user = await _accounts.GetUser(query.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var priceResponse = await _brokerage.GetQuote(user.State, query.Ticker);

                if (stock.State.OpenPosition != null && priceResponse.IsOk)
                {
                    stock.State.OpenPosition.SetPrice(priceResponse.Success.Price);
                }

                var positions = stock.State.GetAllPositions();

                return stock.State.OpenPosition switch {
                    null => new StockOwnershipView(id: stock.State.Id, currentPosition: null, ticker: stock.State.Ticker, positions: positions), 
                    not null => new StockOwnershipView(id: stock.State.Id, currentPosition: stock.State.OpenPosition, ticker: stock.State.Ticker, positions: positions)
                };
            }
        }
    }
}