using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;

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
            private readonly IAccountStorage _accounts;
            
            public Handler(
                IAccountStorage accounts,
                IPortfolioStorage storage
                ) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<OwnedOptionView> Handle(Query request, CancellationToken cancellationToken)
            {
                var _ = await _accounts.GetUser(request.UserId) ?? throw new InvalidOperationException("User not found");
                var option = await _storage.GetOwnedOption(request.Id, request.UserId);

                return new OwnedOptionView(option.State, optionDetail: null);
            }
        }
    }
}