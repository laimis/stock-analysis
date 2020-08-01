using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Options
{
    public class OptionTransactionHandler : 
        MediatR.INotificationHandler<OptionSold>,
        MediatR.INotificationHandler<OptionPurchased>
    {
        private IPortfolioStorage _storage;
        private IMediator _mediator;

        public OptionTransactionHandler(IPortfolioStorage storage, IMediator mediator)
        {
            _storage = storage;
            _mediator = mediator;
        }

        public async Task Handle(OptionSold e, CancellationToken cancellationToken)
        {
            var o = await _storage.GetOwnedOption(e.AggregateId, e.UserId);
            if (o == null)
            {
                return;
            }

            var when = e.When;
            var notes = e.Notes;
            var ticker = o.State.Ticker;

            await CreateNote(e.UserId, when, notes, ticker);
        }

        public async Task Handle(OptionPurchased e, CancellationToken cancellationToken)
        {
            var o = await _storage.GetOwnedOption(e.AggregateId, e.UserId);
            if (o == null)
            {
                return;
            }

            var when = e.When;
            var notes = e.Notes;
            var ticker = o.State.Ticker;

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