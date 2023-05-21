using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
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
            private IAccountStorage _accountStorage;
            private IBrokerage _brokerage;

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
                // check if there is already a pending position for this ticker
                var existing = await _storage.GetPendingStockPositions(request.UserId);
                var found = existing.SingleOrDefault(x => x.State.Id == request.PositionId);
                if (found == null)
                {
                    throw new Exception("Position not found");
                }

                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // get orders for this position
                var ordersResponse = await _brokerage.GetOrders(user.State);
                if (!ordersResponse.IsOk)
                {
                    throw new Exception("Unable to get orders");
                }

                // orders for ticker
                var tickerOrders = ordersResponse.Success.Where(x => x.Ticker == found.State.Ticker && x.IsBuyOrder);
                foreach(var tickerOrder in tickerOrders)
                {
                    var cancelResponse = await _brokerage.CancelOrder(user.State, tickerOrder.OrderId);
                    if (!cancelResponse.IsOk)
                    {
                        throw new Exception("Unable to cancel order");
                    }
                }

                await _storage.DeletePendingStockPosition(found, request.UserId);

                return Unit.Value;
            }
        }
    }
}