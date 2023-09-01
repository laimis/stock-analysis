using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;

namespace core.Options
{
    public class Details
    {
        public class Query : RequestWithUserId<CommandResponse<OwnedOptionView>>
        {
            public Guid Id { get; }

            public Query(Guid id, Guid userId) : base(userId)
            {
                Id = id;
            }
        }

        public class Handler : HandlerWithStorage<Query, CommandResponse<OwnedOptionView>>
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

            public override async Task<CommandResponse<OwnedOptionView>> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId) ?? throw new InvalidOperationException("User not found");
                var option = await _storage.GetOwnedOption(request.Id, request.UserId);
                var chain = await _brokerage.GetOptions(user.State, option.State.Ticker);
                var detail = chain.Success?.FindMatchingOption(
                    strikePrice: option.State.StrikePrice,
                    expirationDate: option.State.ExpirationDate,
                    optionType: option.State.OptionType
                );
                
                var model = new OwnedOptionView(option.State, optionDetail: detail);

                return CommandResponse<OwnedOptionView>.Success(model);
            }
        }
    }
}