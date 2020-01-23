using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Notes.Output;
using core.Shared;
using MediatR;

namespace core.Notes
{
    public class List
    {
        public class Query : RequestWithUserId<NotesList>
        {
            public Query(string userId, string ticker) : base(userId)
            {
                this.Ticker = ticker;
            }

            public string Ticker { get; }
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
                        .Where(n => n.State.RelatedToTicker == (request.Ticker != null ? request.Ticker : n.State.RelatedToTicker))
                        .OrderByDescending(n => n.State.Created)
                );
            }
        }
    }
}