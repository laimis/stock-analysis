using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class DeletePosition
    {
        public class Command : RequestWithUserId<Unit>
        {
            public Command(int positionId, string ticker, Guid userId) : base(userId)
            {
                PositionId = positionId;
                Ticker = ticker;
            }

            public int PositionId { get; }
            public string Ticker { get; }
        }

        public class Handler : HandlerWithStorage<Command, Unit>
        {
            private IAccountStorage _accounts;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stock = await _storage.GetStock(request.Ticker, request.UserId);
                if (stock == null)
                {
                    throw new Exception("Stock not found");
                }

                stock.DeletePosition(request.PositionId);

                await _storage.Save(stock, request.UserId);

                return new Unit();
            }
        }
    }
}