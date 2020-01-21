using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Notes.Output;
using MediatR;

namespace core.Notes
{
    public class List
    {
        public class Query : IRequest<NotesList>
        {
            public Query(string userId)
            {
                this.UserId = userId;
            }

            public string UserId { get; }
        }

        public class Handler : HandlerWithStorage<Query, NotesList>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<NotesList> Handle(Query request, CancellationToken cancellationToken)
            {
                var notes = await _storage.GetNotes(request.UserId);

                return Mapper.MapNotes(
                    notes
                        .Where(n => !n.State.IsArchived)
                        .OrderByDescending(n => n.State.Created)
                );
            }
        }
    }
}