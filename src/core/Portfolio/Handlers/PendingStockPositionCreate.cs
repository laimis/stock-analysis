using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;

namespace core.Portfolio.Handlers
{
    public class PendingStockPositionCreate
    {
        public class Command : RequestWithUserId<PendingStockPositionState>
        {
            [Required]
            public string Notes { get; set; }
            [Required]
            [Range(1, int.MaxValue)]
            public decimal NumberOfShares { get; set; }
            [Required]
            [Range(0.01, int.MaxValue)]
            public decimal Price { get; set; }
            [Range(0, int.MaxValue)]
            public decimal? StopPrice { get; set; }
            [Required]
            public string Ticker { get; set; }
            [Required(AllowEmptyStrings = false)]
            public string Strategy { get; set; }
        }

        public class Handler : HandlerWithStorage<Command, PendingStockPositionState>
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

            public override async Task<PendingStockPositionState> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // check if there is already a pending position for this ticker
                var existing = await _storage.GetPendingStockPositions(request.UserId);
                var found = existing.SingleOrDefault(x => x.State.Ticker == new Ticker(request.Ticker) && x.State.IsClosed == false);
                if (found != null)
                {
                    throw new Exception("There is already a pending position for this ticker");
                }

                await _brokerage.BuyOrder(
                    user: user.State,
                    ticker: request.Ticker,
                    numberOfShares: request.NumberOfShares,
                    price: request.Price,
                    BrokerageOrderType.Limit,
                    BrokerageOrderDuration.GtcPlus
                    );

                var pending = new PendingStockPosition(
                    notes: request.Notes,
                    numberOfShares: request.NumberOfShares,
                    price: request.Price,
                    stopPrice: request.StopPrice,
                    strategy: request.Strategy,
                    ticker: request.Ticker,
                    userId: request.UserId);

                await _storage.Save(pending, request.UserId);

                return pending.State;
            }
        }
    }
}