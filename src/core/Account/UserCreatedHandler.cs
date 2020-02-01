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
            var u = await _storage.GetUser(e.AggregateId.ToString());
            if (u == null)
            {
                return;
            }

            var request = new ProcessIdToUserAssociation(e.AggregateId, e.When);

            await _storage.SaveUserAssociation(request);

            var confirmurl = $"{EmailSettings.ConfirmAccountUrl}/{request.Id}";

            await _email.Send(
                u.State.Email,
                EmailSettings.TemplateConfirmAccount,
                new {confirmurl}
            );
        }
    }
}