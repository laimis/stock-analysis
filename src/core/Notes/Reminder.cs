using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Notes
{
    public class Reminder
    {
        public class Command : RequestWithUserId
        {
            [Required]
            public string Id { get; set; }
            public DateTimeOffset? ReminderDate { get; set; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private IPortfolioStorage _storage;

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var note = await _storage.GetNote(request.UserId, request.Id);

                if (note == null)
                {
                    return new Unit();
                }

                if (request.ReminderDate == null)
                {
                    note.ClearReminder();
                }
                else
                {
                    note.SetupReminder(request.ReminderDate.Value);
                }

                await _storage.Save(note);

                return new Unit();
            }
        }
    }
}