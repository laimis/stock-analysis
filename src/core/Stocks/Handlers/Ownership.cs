using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Portfolio.Output;
using core.Shared;
using core.Stocks.View;

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

                return new StockOwnershipView {
                    Id = stock.State.Id,
                    AverageCost = stock.State.AverageCost,
                    Cost = stock.State.Cost,
                    Owned = stock.State.Owned,
                    Ticker = stock.State.Ticker,
                    Category = stock.State.Category,
                    Transactions = new TransactionList(
                        stock.State.Transactions.Where(t => !t.IsPL), null, null
                    )
                };
            }
        }
    }
}