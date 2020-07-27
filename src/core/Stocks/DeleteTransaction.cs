using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Stocks
{
    public class DeleteTransaction
    {
        public class Command : RequestWithUserId<CommandResponse<OwnedStock>>
        {
            public Command(Guid stockId, Guid transactionId, Guid userId) : base(userId)
            {
                this.Id = stockId;
                this.TransactionId = transactionId;
            }

            public Guid Id { get; set; }
            public Guid TransactionId { get; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse<OwnedStock>>
        {
            private IPortfolioStorage _storage;

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }
            
            public async Task<CommandResponse<OwnedStock>> Handle(
                Command cmd,
                CancellationToken cancellationToken)
            {
                var stock = await _storage.GetStock(cmd.Id, cmd.UserId);
                if (stock == null)
                {
                    return CommandResponse<OwnedStock>.Failed("Trying to delete not owned stock");
                }

                stock.DeleteTransaction(cmd.TransactionId);

                await this._storage.Save(stock, cmd.UserId);

                return CommandResponse<OwnedStock>.Success(stock);
            }
        }
    }
}