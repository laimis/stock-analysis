using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Storage;

namespace core.Brokerage
{
    public class GetAccount
    {
        public class Query : RequestWithUserId<TradingAccount>
        {
            public Query(Guid userId)
            {
                UserId = userId;
            }
        }

        public class Handler : HandlerWithStorage<Query, TradingAccount>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;

            public Handler(IAccountStorage accounts, IBrokerage brokerage, IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }

            public override async Task<TradingAccount> Handle(Query cmd, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(cmd.UserId);
                if (user == null)
                {
                    throw new System.Exception("User not found");
                }

                if (!user.State.ConnectedToBrokerage)
                {
                    throw new System.Exception("User not connected to brokerage");
                }

                var account = await _brokerage.GetAccount(user.State);
                if (!account.IsOk)
                {
                    throw new System.Exception(account.Error.Message);
                }

                return account.Success; 
                    // orders.Success
                    //     .Where(o => o.IncludeInResponses)
                    //     .OrderBy(o => o.StatusOrder)
                    //     .ThenBy(o => o.Ticker)
                    //     .ThenBy(o => o.Date)
                    //     .ToArray();
            }
        }
    }
}