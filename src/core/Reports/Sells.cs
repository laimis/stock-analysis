using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Reports.Views;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Storage;

namespace core.Reports
{
    public class Sells
    {
        public class Query : RequestWithUserId<SellsView>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, SellsView>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;

            public Handler(IAccountStorage accounts, IBrokerage brokerage, IPortfolioStorage storage) : base(storage) =>
                (_accounts, _brokerage) = (accounts, brokerage);

            public override async Task<SellsView> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stocks = await _storage.GetStocks(request.UserId);

                var pricesResult = await _brokerage.GetQuotes(
                    user.State,
                    stocks.Select(s => s.State.Ticker)
                );

                var prices = pricesResult.IsOk switch
                {
                    false => new Dictionary<string, StockQuote>(),
                    true => pricesResult.Success!
                };

                return SellsView.Create(stocks, prices);
            }
        }
    }
}