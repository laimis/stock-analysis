using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared;
using core.Shared.Adapters.Brokerage;

namespace core.Options
{
    public class Details
    {
        public class Query : RequestWithUserId<OwnedOptionView>
        {
            public Guid Id { get; }

            public Query(Guid id, Guid userId) : base(userId)
            {
                Id = id;
            }
        }

        public class Handler : HandlerWithStorage<Query, OwnedOptionView>
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

            public override async Task<OwnedOptionView> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }
                
                var option = await _storage.GetOwnedOption(request.Id, request.UserId);

                var price = await _brokerage.GetQuote(user.State, option.State.Ticker);
                
                return price.IsOk switch {
                    true => new OwnedOptionView(option.State, price.Success.Price),
                    false => new OwnedOptionView(option.State)
                };
            }
        }
    }
}