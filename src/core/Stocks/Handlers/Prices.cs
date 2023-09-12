using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Storage;
using core.Stocks.View;
using MediatR;

namespace core.Stocks.Handlers
{
    public class Prices
    {
        public class Query : IRequest<CommandResponse<PricesView>>
        {
            public int NumberOfDays { get; }
            public string Ticker { get; }
            public Guid UserId { get; }
            public DateTimeOffset Start { get; }
            public DateTimeOffset End { get; }

            public Query(int numberOfDays, string ticker, Guid userId)
            {
                Ticker = ticker;
                UserId = userId;

                var totalDays = numberOfDays + 200; // to make sure we have enough for the moving averages

                Start = DateTimeOffset.UtcNow.AddDays(-totalDays);
                End = DateTimeOffset.UtcNow;
            }

            public Query(DateTimeOffset start, DateTimeOffset end, string ticker, Guid userId)
            {
                Start = start;
                End = end;
                Ticker = ticker;
                UserId = userId;
            }
        }

        public class Handler : IRequestHandler<Query, CommandResponse<PricesView>>
        {
            private IAccountStorage _storage;
            private IBrokerage _brokerage;

            public Handler(IAccountStorage storage, IBrokerage brokerage)
            {
                _storage = storage;
                _brokerage = brokerage;
            }

            public async Task<CommandResponse<PricesView>> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }
                
                var prices = await _brokerage.GetPriceHistory(user.State, request.Ticker, start: request.Start, end: request.End);
                if (!prices.IsOk)
                {
                    CommandResponse<PricesView>.Failed(prices.Error.Message);
                }

                return CommandResponse<PricesView>.Success(
                    new PricesView(
                        prices.Success
                    )
                );
            }
        }
    }
}