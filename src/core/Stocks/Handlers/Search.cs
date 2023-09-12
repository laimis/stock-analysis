using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Stocks.Handlers
{
    public class Search
    {
        public class Query : RequestWithUserId<SearchResult[]>
        {
            public string Term { get; private set; }

            public Query(string term, Guid userId) : base(userId)
            {
                Term = term;
            }
        }

        public class Handler : IRequestHandler<Query, SearchResult[]>
        {
            private IAccountStorage _accountStorage;
            private IBrokerage _brokerage;

            public Handler(IAccountStorage accountStorage, IBrokerage brokerage)
            {
                _accountStorage = accountStorage;
                _brokerage = brokerage;
            }

            public async Task<SearchResult[]> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var results = await _brokerage.Search(user.State, request.Term);
                if (!results.IsOk)
                {
                    throw new InvalidOperationException(results.Error.Message);
                }

                return results.Success;
            }
        }
    }
}