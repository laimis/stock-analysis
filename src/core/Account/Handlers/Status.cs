using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account.Responses;
using core.Adapters.Emails;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class Status
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : MediatR.IRequestHandler<Query, object>,
            INotificationHandler<UserStatusRecalculate>
        {
            private readonly IAccountStorage _storage;

            public Handler(IAccountStorage storage)
            {
                _storage = storage;
            }

            public async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var cached = await _storage.ViewModel<AccountStatusView>(request.UserId);
                if (cached != null)
                {
                    cached.LoggedIn = true;
                    return cached;
                }

                return await GetFromDatabase(request.UserId);
            }

            public async Task Handle(UserStatusRecalculate notification, CancellationToken cancellationToken)
            {
                var status = await GetFromDatabase(notification.UserId);

                await _storage.SaveViewModel(status, notification.UserId);
            }

            private async Task<AccountStatusView> GetFromDatabase(Guid userId)
            {
                var user = await _storage.GetUser(userId);
                if (user == null)
                {
                    return new AccountStatusView
                    {
                        LoggedIn = false,
                    };
                }

                return new AccountStatusView
                {
                    LoggedIn = true,
                    Username = user.Id,
                    Verified = user.State.Verified != null,
                    Created = user.State.Created,
                    Email = user.State.Email,
                    Firstname = user.State.Firstname,
                    Lastname = user.State.Lastname,
                    IsAdmin = user.State.Email == EmailSettings.Admin.Email,
                    SubscriptionLevel = user.State.SubscriptionLevel,
                    ConnectedToBrokerage = user.State.ConnectedToBrokerage,
                    BrokerageAccessTokenExpired = user.State.BrokerageAccessTokenExpired
                };
            }
        }
    }
}