using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Notes
{
    public class Archive
    {
        public class Command : IRequest
        {
            [Required]
            public string Id { get; set; }
            public string UserId { get; private set; }
            public void WithUserId(string userId) => UserId = userId;
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

                note.Archive();

                await _storage.Save(note);

                return new Unit();
            }
        }
    }
}