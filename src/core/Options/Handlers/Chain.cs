using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Options;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using MediatR;

namespace core.Options
{
    public class Chain
    {
        public class Query : RequestWithUserId<CommandResponse<OptionDetailsViewModel>>
        {
            public Query(Ticker ticker, Guid userId) : base(userId)
            {
                Ticker = ticker;
            }

            public Ticker Ticker { get; }
        }

        public class Handler : IRequestHandler<Query, CommandResponse<OptionDetailsViewModel>>
        {
            private readonly IAccountStorage _accounts;
            private readonly IBrokerage _brokerage;
            
            public Handler(IAccountStorage accounts, IBrokerage brokerage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }
            
            public async Task<CommandResponse<OptionDetailsViewModel>> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId) ?? throw new InvalidOperationException("User not found");
                var priceResult = await _brokerage.GetQuote(user.State, request.Ticker);
                var price = priceResult.IsOk switch {
                    true => priceResult.Success.Price,
                    false => (decimal?)null
                };

                var detailsResponse = await _brokerage.GetOptions(user.State, request.Ticker);
                if (!detailsResponse.IsOk)
                {
                    return CommandResponse<OptionDetailsViewModel>.Failed(detailsResponse.Error.Message);
                }

                var model = MapOptionDetails(price, detailsResponse.Success!);

                return CommandResponse<OptionDetailsViewModel>.Success(model);
            }
        }

        public static OptionDetailsViewModel MapOptionDetails(
            decimal? price,
            OptionChain chain)
        {
            return new OptionDetailsViewModel
            {
                StockPrice = price,
                Options = chain.Options,
                Expirations = chain.Options.Select(o => o.ExpirationDate).Distinct().ToArray(),
                Volatility = chain.Volatility,
                NumberOfContracts = chain.NumberOfContracts,
            };
        }
    }
}