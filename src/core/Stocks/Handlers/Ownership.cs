using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
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
            private IStocksService2 _stocksService;

            public Handler(
                IAccountStorage accounts,
                IStocksService2 stockService,
                IPortfolioStorage storage
                ) : base(storage)
            {
                _accounts = accounts;
                _stocksService = stockService;
            }

            public override async Task<StockOwnershipView> Handle(Query query, CancellationToken cancellationToken)
            {
                var stock = await _storage.GetStock(query.Ticker, query.UserId);
                if (stock == null)
                {
                    return null;
                }

                var transactions = stock.State.Transactions.Where(t => !t.IsPL)
                    .OrderByDescending(t => t.Date)
                    .ToList();

                var priceResponse = await _stocksService.GetPrice(query.Ticker);

                if (stock.State.OpenPosition != null && priceResponse.IsOk)
                {
                    stock.State.OpenPosition.SetPrice(priceResponse.Success.Amount);
                }

                return stock.State.OpenPosition switch {
                    null => new StockOwnershipView(id: stock.State.Id, position: null, ticker: stock.State.Ticker, transactions: transactions), 
                    not null => new StockOwnershipView(id: stock.State.Id, position: stock.State.OpenPosition, ticker: stock.State.Ticker, transactions: transactions)
                };
            }
        }
    }
}