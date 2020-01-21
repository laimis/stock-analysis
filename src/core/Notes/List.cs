using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Notes
{
    public class List
    {
        public class Query : IRequest<object>
        {
            public Query(string userId)
            {
                this.UserId = userId;
            }

            public string UserId { get; }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var notes = await _storage.GetNotes(request.UserId);

                return Mapper.MapNotes(notes.OrderByDescending(n => n.State.Created));
            }
        }
    }
}