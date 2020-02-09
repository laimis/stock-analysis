using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Notes.Output;
using core.Shared;

namespace core.Notes
{
    public class List
    {
        public class Query : RequestWithUserId<NotesList>
        {
            public Query(Guid userId, string ticker) : base(userId)
            {
                if (ticker != null)
                {
                    this.Ticker = ticker;
                }
            }

            public Ticker? Ticker { get; }
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
                        .Where(n => n.MatchesTickerFilter(request.Ticker))
                        .OrderByDescending(n => n.State.Created)
                );
            }
        }
    }
}