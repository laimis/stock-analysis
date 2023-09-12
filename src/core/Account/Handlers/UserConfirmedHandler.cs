using System.Threading;
using System.Threading.Tasks;
using core.Shared.Adapters.Emails;

namespace core.Account.Handlers
{
    public class UserConfirmedHandler : MediatR.INotificationHandler<UserConfirmed>
    {
        private IAccountStorage _storage;
        private IEmailService _email;

        public UserConfirmedHandler(IAccountStorage storage, IEmailService email)
        {
            _storage = storage;
            _email = email;
        }

        public async Task Handle(UserConfirmed e, CancellationToken cancellationToken)
        {
            var u = await _storage.GetUser(e.AggregateId);
            if (u == null)
            {
                return;
            }

            await _email.Send(
                new Recipient(email: u.State.Email, name: u.State.Name),
                Sender.Support,
                EmailTemplate.NewUserWelcome,
                new {}
            );
        }
    }
}