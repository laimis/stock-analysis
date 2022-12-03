using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;

namespace core.Brokerage
{
    public class Orders
    {
        public class Query : RequestWithUserId<Order[]>
        {
            public Query(Guid userId)
            {
                UserId = userId;
            }
        }

        public class Handler : HandlerWithStorage<Query, Order[]>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;

            public Handler(IAccountStorage accounts, IBrokerage brokerage, IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }

            public override async Task<Order[]> Handle(Query cmd, CancellationToken cancellationToken)
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

                var orders = await _brokerage.GetOrders(user.State);
                if (!orders.IsOk)
                {
                    throw new System.Exception(orders.Error.Message);
                }

                return 
                    orders.Success
                        .Where(o => o.IncludeInResponses)
                        .OrderBy(o => o.StatusOrder)
                        .ToArray();
            }
        }
    }
}