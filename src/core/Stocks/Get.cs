using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;

namespace core.Stocks
{
    public class Get
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(string ticker)
            {
                this.Ticker = ticker;
            }

            public string Ticker { get; }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            private IAccountStorage _accounts;

            public Handler(IPortfolioStorage storage, IAccountStorage accounts) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<object> Handle(Query query, CancellationToken cancellationToken)
            {
                var stock = await this._storage.GetStock(query.Ticker, query.UserId);
                if (stock == null)
                {
                    return null;
                }

                return stock.State;
            }
        }
    }
}