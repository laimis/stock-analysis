using System.Threading;
using System.Threading.Tasks;
using core.Cryptos.Views;
using core.Shared.Adapters.Cryptos;

namespace core.Cryptos.Handlers
{
    public class Details
    {
        public class Query : RequestWithToken<CryptoDetailsView>
        {
            public Query(string token)
            {
                Token = token;
            }
        }

        public class Handler : HandlerWithStorage<Query, CryptoDetailsView>
        {
            private ICryptoService _prices;

            public Handler(IPortfolioStorage storage, ICryptoService prices) : base(storage)
            {
                _prices = prices;
            }

            public override async Task<CryptoDetailsView> Handle(Query query, CancellationToken cancellationToken)
            {
                var prices = await _prices.Get();
                prices.TryGet(query.Token, out var price);
                return new CryptoDetailsView(query.Token, price);
            }
        }
    }
}