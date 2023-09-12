using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class Routines
    {
        public class Query : RequestWithUserId<RoutineState[]>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : IRequestHandler<Query, RoutineState[]>
        {
            private IAccountStorage _accountsStorage;
            private IPortfolioStorage _portfolioStorage;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage)
            {
                _accountsStorage = accounts;
                _portfolioStorage = storage;
            }

            public async Task<RoutineState[]> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountsStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var lists = await _portfolioStorage.GetRoutines(user.State.Id);
                return lists
                    .Select(x => x.State)
                    .OrderBy(x => x.Name.ToLower())
                    .ToArray();
            }
        }
    }
}