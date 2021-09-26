using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Cryptos.Views;
using core.Shared;

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
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override Task<CryptoDashboardView> Handle(Query query, CancellationToken cancellationToken)
            {
                return LoadFromDb(query.UserId);
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