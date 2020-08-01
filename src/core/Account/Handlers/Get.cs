using System;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Emails;
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
                    verified = user.State.Verified != null,
                    created = user.State.Created,
                    email = user.State.Email,
                    firstname = user.State.Firstname,
                    lastname = user.State.Lastname,
                    isAdmin = user.State.Email == EmailSettings.Admin,
                    subscriptionLevel = user.State.SubscriptionLevel
                };
            }
        }
    }
}