using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class PendingStockPositionClose
    {
        public class Command : RequestWithUserId
        {
            public Command(Guid positionId, Guid userId) : base(userId)
            {
                PositionId = positionId;
            }

            public Guid PositionId { get; }
        }

        public class Handler : HandlerWithStorage<Command, Unit>
        {
            private readonly IAccountStorage _accountStorage;
            private readonly IBrokerage _brokerage;

            public Handler(
                IBrokerage brokerage,
                IAccountStorage accountStorage,
                IPortfolioStorage storage) : base(storage)
            {
                _accountStorage = accountStorage;
                _brokerage = brokerage;
            }

            public override async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var existing = await _storage.GetPendingStockPositions(request.UserId);
                var found = existing.SingleOrDefault(x => x.State.Id == request.PositionId)
                    ?? throw new Exception("Position not found");
                var user = await _accountStorage.GetUser(request.UserId)
                    ?? throw new Exception("User not found");

                // get orders for this position
                var accountResponse = await _brokerage.GetAccount(user.State);
                if (!accountResponse.IsOk)
                {
                    throw new Exception("Unable to get orders");
                }

                // orders for ticker
                var tickerOrders = accountResponse.Success.Orders.Where(x => x.Ticker == found.State.Ticker && x.IsBuyOrder && x.IsActive);
                foreach(var tickerOrder in tickerOrders)
                {
                    var cancelResponse = await _brokerage.CancelOrder(user.State, tickerOrder.OrderId);
                    if (!cancelResponse.IsOk)
                    {
                        throw new Exception("Unable to cancel order");
                    }
                }

                found.Close(purchased: false);

                await _storage.Save(found, request.UserId);

                return Unit.Value;
            }
        }
    }
}