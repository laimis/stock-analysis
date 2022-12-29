using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using MediatR;

namespace core.Stocks.Handlers
{
    public class Price
    {
        public class Query : RequestWithUserId<decimal?>
        {
            public Ticker Ticker { get; }

            public Query(string ticker, Guid userId) : base(userId)
            {
                Ticker = ticker;
            }
        }

        public class Handler : IRequestHandler<Query, decimal?>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;

            public Handler(IAccountStorage accounts, IBrokerage brokerage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }

            public async Task<decimal?> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var price = await _brokerage.GetQuote(user.State, request.Ticker);

                return price.Success?.lastPrice;
            }
        }
    }
}