using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Cryptos.Views;
using core.Shared;
using core.Shared.Adapters.Cryptos;
using core.Shared.Adapters.Storage;

namespace core.Cryptos.Handlers
{
    public class Dashboard
    {
        public class Query : RequestWithUserId<CryptoDashboardView>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, CryptoDashboardView>
        {
            private ICryptoService _prices;

            public Handler(IPortfolioStorage storage, ICryptoService prices) : base(storage)
            {
                _prices = prices;
            }

            public override async Task<CryptoDashboardView> Handle(Query query, CancellationToken cancellationToken)
            {
                var dashboardView = await LoadFromDb(query.UserId);

                var prices = await _prices.Get(dashboardView.Owned.Select(o => o.Token));

                foreach(var owned in dashboardView.Owned)
                {
                    if (prices.TryGetValue(owned.Token, out var price))
                    {
                        owned.ApplyPrice(price);
                    }
                }

                return dashboardView;
            }

            private async Task<CryptoDashboardView> LoadFromDb(Guid userId)
            {
                var cryptos = await _storage.GetCryptos(userId);

                var ownedStocks = cryptos
                    .Where(c => c.State.Quantity > 0)
                    .Select(c => new OwnedCryptoView(c))
                    .OrderByDescending(c => c.Cost)
                    .ToList();

                return new CryptoDashboardView(ownedStocks);
            }
        }
    }
}