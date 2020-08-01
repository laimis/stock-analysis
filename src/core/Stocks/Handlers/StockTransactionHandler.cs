using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Stocks
{
    public class StockTransactionHandler : 
        MediatR.INotificationHandler<StockSold>,
        MediatR.INotificationHandler<StockPurchased>
    {
        private IPortfolioStorage _storage;
        private IMediator _mediator;

        public StockTransactionHandler(IPortfolioStorage storage, IMediator mediator)
        {
            _storage = storage;
            _mediator = mediator;
        }

        public async Task Handle(StockSold e, CancellationToken cancellationToken)
        {
            var s = await _storage.GetStock(e.Ticker, e.UserId);
            if (s == null)
            {
                return;
            }

            var when = e.When;
            var notes = e.Notes;
            var ticker = e.Ticker;

            await CreateNote(e.UserId, when, notes, ticker);
        }

        public async Task Handle(StockPurchased e, CancellationToken cancellationToken)
        {
            var s = await _storage.GetStock(e.Ticker, e.UserId);
            if (s == null)
            {
                return;
            }

            var when = e.When;
            var notes = e.Notes;
            var ticker = e.Ticker;

            await CreateNote(e.UserId, when, notes, ticker);
        }

        private async Task CreateNote(Guid userId, DateTimeOffset when, string notes, string ticker)
        {
            if (string.IsNullOrEmpty(notes))
            {
                return;
            }

            var cmd = new Notes.Add.Command
            {
                Created = when,
                Note = notes,
                Ticker = ticker
            };

            cmd.WithUserId(userId);

            await _mediator.Send(cmd);
        }
    }
}