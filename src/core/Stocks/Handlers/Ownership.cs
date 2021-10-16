using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Stocks.View;

namespace core.Stocks
{
    public class Ownership
    {
        public class Query : RequestWithUserId<StockOwnershipView>
        {
            public Query(string ticker, bool raw = false)
            {
                Ticker = ticker;
                Raw = raw;
            }

            public string Ticker { get; }
            public bool Raw { get; }
        }

        public class Handler : HandlerWithStorage<Query, StockOwnershipView>
        {
            private IAccountStorage _accounts;

            public Handler(IPortfolioStorage storage, IAccountStorage accounts) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<StockOwnershipView> Handle(Query query, CancellationToken cancellationToken)
            {
                var stock = await _storage.GetStock(query.Ticker, query.UserId);
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
                    Transactions = stock.State.Transactions.Where(t => !t.IsPL)
                        .OrderByDescending(t => t.Date)
                        .ToList()
                };
            }
        }
    }
}