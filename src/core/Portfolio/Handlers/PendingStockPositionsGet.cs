using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Portfolio.Handlers
{
    public class PendingStockPositionsGet
    {
        public class Query : RequestWithUserId<IEnumerable<PendingStockPositionState>>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, IEnumerable<PendingStockPositionState>>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<IEnumerable<PendingStockPositionState>> Handle(Query query, CancellationToken cancellationToken)
            {
                var positions = await _storage.GetPendingStockPositions(query.UserId);

                return positions.Select(x => x.State);
            }
        }
    }
}