using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

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
        }

        public class Handler : HandlerWithStorage<Command, PendingStockPositionState>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<PendingStockPositionState> Handle(Command request, CancellationToken cancellationToken)
            {
                var pending = new PendingStockPosition(
                    notes: request.Notes,
                    numberOfShares: request.NumberOfShares,
                    price: request.Price,
                    stopPrice: request.StopPrice,
                    ticker: request.Ticker,
                    userId: request.UserId);

                await _storage.Save(pending, request.UserId);

                return pending.State;
            }
        }
    }
}