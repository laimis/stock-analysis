using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using MediatR;

namespace core.Portfolio
{
    public class PositionLabelsSet
    {
        public class Command : RequestWithUserId<Unit>
        {
            [Required]
            public string Ticker { get; set; }
            [Required]
            public int PositionId { get; set; }
            [Required]
            public string Key { get; set; }
            [Required]
            public string Value { get; set; }
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

                stock.SetPositionLabel(request.PositionId, request.Key, request.Value);

                await _storage.Save(stock, request.UserId);

                return new Unit();
            }
        }
    }
}