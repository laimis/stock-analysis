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
            private TradeGrade? _grade;
            [Required]
            public string Grade 
            {
                get 
                { 
                    if (_grade == null) return null;
                    return _grade;
                }
                
                set 
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        _grade = null;
                        return;
                    }
                    _grade = new TradeGrade(value);
                }
            }

            [Required]
            public string Ticker { get; set; }
            [Required]
            public int PositionId { get; set; }
            public string Note { get; set; }
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
                    string.IsNullOrWhiteSpace(request.Note) ? null : request.Note
                );

                await _storage.Save(stock, request.UserId);

                return new Unit();
            }
        }
    }
}