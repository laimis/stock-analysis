using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Notes
{
    public class Followup
    {
        public class Command : IRequest
        {
            [Required]
            public string Id { get; set; }
            [Required]
            public string Text { get; set; }
            public string UserId { get; private set; }
            public void WithUserId(string userId) => UserId = userId;
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