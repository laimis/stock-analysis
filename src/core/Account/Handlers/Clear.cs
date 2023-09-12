using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Account.Handlers
{
    public class Clear
    {
        public class Command : RequestWithUserId
        {
            public string Feedback { get; set; }
        }

        public class Handler : MediatR.IRequestHandler<Command>
        {
            private IAccountStorage _storage;
            private IPortfolioStorage _portfolio;

            public Handler(
                IAccountStorage storage,
                IPortfolioStorage portfolioStorage)
            {
                _storage = storage;
                _portfolio = portfolioStorage;
            }

            public async Task<Unit> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUser(cmd.UserId);
                if (user == null)
                {
                    return new Unit();
                }

                await _portfolio.Delete(user.Id);
                
                return new Unit();
            }
        }
    }
}