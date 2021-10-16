using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Notes
{
    public class Get
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId, Guid noteId) : base(userId)
            {
                NoteId = noteId;
            }

            public Guid NoteId { get; }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var note = await _storage.GetNote(request.UserId, request.NoteId);
                return note?.State;
            }
        }
    }
}