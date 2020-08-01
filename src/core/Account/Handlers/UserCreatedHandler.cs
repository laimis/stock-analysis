using System;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Emails;

namespace core.Account
{
    public class UserCreatedHandler : MediatR.INotificationHandler<UserCreated>
    {
        private IAccountStorage _storage;
        private IEmailService _email;

        public UserCreatedHandler(IAccountStorage storage, IEmailService email)
        {
            _storage = storage;
            _email = email;
        }

        public async Task Handle(UserCreated e, CancellationToken cancellationToken)
        {
            var u = await _storage.GetUser(e.AggregateId);
            if (u == null)
            {
                return;
            }

            await SendConfirmAccountEmail(e, u);

            await SendNewUserSignedUpEmail(u);
        }

        private async Task SendNewUserSignedUpEmail(User u)
        {
            await _email.Send(
                EmailSettings.Admin,
                Sender.NoReply,
                EmailTemplate.AdminNewUser,
                new { email = u.State.Email }
            );
        }

        private async Task SendConfirmAccountEmail(UserCreated e, User u)
        {
            var request = new ProcessIdToUserAssociation(e.AggregateId, e.When);

            await _storage.SaveUserAssociation(request);

            var confirmurl = $"{EmailSettings.ConfirmAccountUrl}/{request.Id}";

            await _email.Send(
                u.State.Email,
                Sender.NoReply,
                EmailTemplate.ConfirmAccount,
                new { confirmurl }
            );
        }
    }
}