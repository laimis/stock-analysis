using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using MediatR;

namespace core.Stocks.Handlers
{
    public class Quote
    {
        public class Query : RequestWithUserId<StockQuote>
        {
            public Ticker Ticker { get; }

            public Query(string ticker, Guid userId)
            {
                Ticker = ticker;
                UserId = userId;
            }
        }

        public class Handler : IRequestHandler<Query, StockQuote>
        {
            private IAccountStorage _account;
            private IBrokerage _brokerage;

            public Handler(IAccountStorage account, IBrokerage brokerage)
            {
                _account = account;
                _brokerage = brokerage;
            }

            public async Task<StockQuote> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _account.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var quote = await _brokerage.GetQuote(user.State, request.Ticker);
                if (!quote.IsOk)
                {
                    throw new Exception("Failed to get quote");
                }
                return quote.Success;
            }
        }
    }
}