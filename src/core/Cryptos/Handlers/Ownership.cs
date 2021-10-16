using System;
using System.Threading;
using System.Threading.Tasks;
using core.Cryptos.Views;

namespace core.Cryptos.Handlers
{
    public class Ownership
    {
        public class Query : RequestWithToken<CryptoOwnershipView>
        {
            public Query(string token, Guid userId)
            {
                Token = token;
                WithUserId(userId);
            }
        }

        public class Handler : HandlerWithStorage<Query, CryptoOwnershipView>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<CryptoOwnershipView> Handle(Query query, CancellationToken cancellationToken)
            {
                var crypto = await _storage.GetCrypto(query.Token, query.UserId);
                if (crypto == null)
                {
                    return null;
                }

                return new CryptoOwnershipView(crypto.State);
            }
        }
    }
}