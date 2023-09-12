using System;
using System.Threading;
using System.Threading.Tasks;
using core.Cryptos.Views;
using core.Shared;
using core.Shared.Adapters.Storage;

namespace core.Cryptos.Handlers
{
    public class DeleteTransaction
    {
        public class Command : RequestWithToken<bool>
        {
            public Command(string token, Guid transactionId, Guid userId)
            {
                Token = token;
                TransactionId = transactionId;
                WithUserId(userId);
            }

            public Guid TransactionId { get; }
        }

        public class Handler : HandlerWithStorage<Command, bool>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<bool> Handle(Command query, CancellationToken cancellationToken)
            {
                var crypto = await _storage.GetCrypto(query.Token, query.UserId);
                if (crypto == null)
                {
                    return false;
                }

                crypto.DeleteTransaction(query.TransactionId);

                await _storage.Save(crypto, query.UserId);

                return true;
            }
        }
    }
}