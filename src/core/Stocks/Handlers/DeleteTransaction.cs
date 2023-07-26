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
            public Command(string ticker, Guid transactionId, Guid userId) : base(userId)
            {
                Ticker = ticker;
                TransactionId = transactionId;
            }

            public Ticker Ticker { get; }
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
                var stock = await _storage.GetStock(cmd.Ticker, cmd.UserId);
                if (stock == null)
                {
                    return CommandResponse<OwnedStock>.Failed("Trying to delete not owned stock");
                }

                stock.DeleteTransaction(cmd.TransactionId);

                await _storage.Save(stock, cmd.UserId);

                return CommandResponse<OwnedStock>.Success(stock);
            }
        }
    }
}