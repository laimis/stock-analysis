using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Account
{
    public class GetLogins
    {
        public class Query : IRequest<IEnumerable<LoginLogEntry>>
        {
        }

        public class Handler : IRequestHandler<Query, IEnumerable<LoginLogEntry>>
        {
            private IAccountStorage _storage;

            public Handler(IAccountStorage storage)
            {
                _storage = storage;
            }

            public async Task<IEnumerable<LoginLogEntry>> Handle(Query request, CancellationToken cancellationToken)
            {
                return await this._storage.GetLogins();
            }
        }
    }
}