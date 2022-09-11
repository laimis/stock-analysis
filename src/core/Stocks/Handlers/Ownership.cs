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
            public Query(string ticker)
            {
                Ticker = ticker;
            }

            public string Ticker { get; }
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

                var openPosition = stock.State.OpenPosition;
                if (openPosition == null)
                {
                    return null;
                }

                return new StockOwnershipView {
                    Id = stock.State.Id,
                    AverageCost = openPosition.AverageCostPerShare,
                    Cost = openPosition.Cost,
                    Owned = openPosition.NumberOfShares,
                    Ticker = openPosition.Ticker,
                    Category = openPosition.Category,
                    Transactions = stock.State.Transactions.Where(t => !t.IsPL)
                        .OrderByDescending(t => t.Date)
                        .ToList()
                };
            }
        }
    }
}