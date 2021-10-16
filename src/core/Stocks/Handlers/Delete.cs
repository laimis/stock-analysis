using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Stocks
{
    public class Delete
    {
        public class Command : RequestWithUserId<CommandResponse<OwnedStock>>
        {
            public Command(Guid stockId, Guid userId)
            {
                Id = stockId;
                WithUserId(userId);
            }

            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse<OwnedStock>>
        {
            private IPortfolioStorage _storage;

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }
            
            public async Task<CommandResponse<OwnedStock>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var stock = await _storage.GetStock(cmd.Id, cmd.UserId);
                if (stock == null)
                {
                    return CommandResponse<OwnedStock>.Failed("Trying to delete not owned stock");
                }

                stock.Delete();

                await _storage.Save(stock, cmd.UserId);

                return CommandResponse<OwnedStock>.Success(stock);
            }
        }
    }
}