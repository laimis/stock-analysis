using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Account
{
    public class Get
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : MediatR.IRequestHandler<Query, object>
        {
            private IAccountStorage _storage;

            public Handler(IAccountStorage storage)
            {
                _storage = storage;
            }

            public async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                Console.WriteLine("getting user " + request.UserId);

                var user = await _storage.GetUser(request.UserId);
                
                Console.WriteLine("returning " + user.State.Id);
                
                return user.State;
            }
        }
    }
}