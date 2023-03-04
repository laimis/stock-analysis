using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Stocks.View;
using MediatR;

namespace core.Stocks
{
    public class Details
    {
        public class Query : RequestWithUserId<StockDetailsView>
        {
            public string Ticker { get; }

            public Query(string ticker, Guid userId) : base(userId)
            {
                Ticker = ticker;
            }
        }

        public class Handler : IRequestHandler<Query, StockDetailsView>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;

            public Handler(IAccountStorage accounts, IBrokerage brokerage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }

            public async Task<StockDetailsView> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var profileResponse = await _brokerage.GetStockProfile(user.State, request.Ticker);
                var price = await _brokerage.GetQuote(user.State, request.Ticker);

                return new StockDetailsView
                {
                    Ticker = request.Ticker,
                    Price = price.Success?.Price,
                    Profile = profileResponse.Success
                };
            }
        }
    }
}