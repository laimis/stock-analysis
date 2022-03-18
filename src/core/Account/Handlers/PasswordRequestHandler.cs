using System;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Emails;

namespace core.Account
{
    public class PasswordRequestHandler : MediatR.INotificationHandler<UserPasswordResetRequested>
    {
        private IAccountStorage _storage;
        private IEmailService _email;

        public PasswordRequestHandler(IAccountStorage storage, IEmailService email)
        {
            _storage = storage;
            _email = email;
        }

        public async Task Handle(UserPasswordResetRequested e, CancellationToken cancellationToken)
        {
            // generate random guid that maps back to aggregate id
            // store it in the storage
            // send email

            Console.WriteLine("Issuing password reset");

            var u = await _storage.GetUser(e.AggregateId);
            if (u == null)
            {
                return;
            }

            var association = new ProcessIdToUserAssociation(e.AggregateId, e.When);

            await _storage.SaveUserAssociation(association);

            var reseturl = $"{EmailSettings.PasswordResetUrl}/{association.Id}";

            await _email.Send(
                new Recipient(email: u.State.Email, name: u.State.Name),
                Sender.NoReply,
                EmailTemplate.PasswordReset,
                new {reseturl}
            );
        }
    }
}