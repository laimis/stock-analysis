using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class PositionLabelsDelete
    {
        public class Command : RequestWithUserId<Unit>
        {
            public Command(string ticker, int positionId, string key, Guid userId)
            {
                UserId = userId;
                PositionId = positionId;
                Ticker = ticker;
                Key = key;
            }

            public int PositionId { get; }
            public string Ticker { get; }
            public string Key { get; }
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

                stock.DeletePositionLabel(request.PositionId, request.Key);

                await _storage.Save(stock, request.UserId);

                return new Unit();
            }
        }
    }
}