using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Portfolio.Views;
using core.Shared;
using core.Shared.Adapters.Storage;

namespace core.Portfolio.Handlers
{
    public class Get
    {
        public class Query : RequestWithUserId<PortfolioView>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler :
            HandlerWithStorage<Query, PortfolioView>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<PortfolioView> Handle(Query request, CancellationToken cancellationToken)
            {
                var userId = request.UserId;
                
                var stocks = await _storage.GetStocks(userId);

                var openStocks = stocks.Where(s => s.State.OpenPosition != null).Select(s => s.State.OpenPosition).ToList();

                var options = await _storage.GetOwnedOptions(userId);

                var openOptions = options
                    .Where(o => o.State.NumberOfContracts != 0 && o.State.DaysUntilExpiration > -5)
                    .OrderBy(o => o.State.Expiration);

                var cryptos = await _storage.GetCryptos(userId);

                var obj = new PortfolioView
                {
                    OpenStockCount = openStocks.Count(),
                    OpenOptionCount = openOptions.Count(),
                    OpenCryptoCount = cryptos.Count()
                };
                return obj;
            }
        }
    }
}