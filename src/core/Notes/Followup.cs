using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Notes
{
    public class Followup
    {
        public class Command : RequestWithUserId
        {
            [Required]
            public string Id { get; set; }
            [Required]
            public string Text { get; set; }
        }

        public class Handler : HandlerWithStorage<Command, Unit>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var note = await _storage.GetNote(request.UserId, request.Id);
                if (note == null)
                {
                    return new Unit();
                }

                note.Followup(request.Text);

                await _storage.Save(note);

                return new Unit();
            }
        }
    }
}