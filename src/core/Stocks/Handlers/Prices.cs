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
            public int NumberOfDays { get; }
            public string Ticker { get; }
            public Guid UserId { get; }

            public Query(int numberOfDays, string ticker, Guid userId)
            {
                NumberOfDays = numberOfDays;
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

                var totalDays = request.NumberOfDays + 200; // to make sure we have enough for the moving averages

                var start = DateTimeOffset.UtcNow.AddDays(-totalDays);
                var end = DateTimeOffset.UtcNow;

                var prices = await _brokerage.GetPriceHistory(user.State, request.Ticker, start: start, end: end);
                if (!prices.IsOk)
                {
                    throw new Exception("Failed to get price history");
                }

                return new PricesView(
                    prices.Success
                );
            }
        }
    }
}