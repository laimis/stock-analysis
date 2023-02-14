using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Stocks;
using core.Stocks.Services.Trading;
using MediatR;

namespace core.Portfolio
{
    public class GradePosition
    {
        public class Command : RequestWithUserId<Unit>
        {
            [Required]
            public TradeGrade Grade { get; }
            [Required]
            public string Ticker { get; }
            [Required]
            public int PositionId { get; }
            public string Note { get; }
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

                stock.AssignGrade(
                    request.PositionId,
                    request.Grade,
                    request.Note
                );

                return new Unit();
            }
        }
    }
}