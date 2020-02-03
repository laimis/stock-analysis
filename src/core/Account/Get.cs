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
                var user = await _storage.GetUser(request.UserId);
                if (user == null)
                {
                    return new
                    {
                        loggedIn = false,
                    };
                }
                
                return new
                {
                    username = user.Id,
                    loggedIn = true,
                    verified = user.Verified,
                    created = user.Created,
                    email = user.Email,
                    firstname = user.Firstname,
                    lastname = user.Lastname
                };
            }
        }
    }
}