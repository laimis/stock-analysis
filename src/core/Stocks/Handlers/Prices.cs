using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared.Adapters.Brokerage;
using core.Stocks.View;
using MediatR;

namespace core.Stocks.Handlers
{
    public class Prices
    {
        public class Query : IRequest<PricesView>
        {
            public string Ticker { get; }
            public Guid UserId { get; }

            public Query(string ticker, Guid userId)
            {
                Ticker = ticker;
                UserId = userId;
            }
        }

        public class Handler : IRequestHandler<Query, PricesView>
        {
            private IAccountStorage _storage;
            private IBrokerage _brokerage;

            public Handler(IAccountStorage storage, IBrokerage brokerage)
            {
                _storage = storage;
                _brokerage = brokerage;
            }

            public async Task<PricesView> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var prices = await _brokerage.GetHistoricalPrices(user.State, request.Ticker);
                if (!prices.IsOk)
                {
                    throw new Exception("Failed to get historical prices");
                }

                return new PricesView(
                    prices.Success
                );
            }
        }
    }
}