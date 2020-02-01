using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Emails;
using MediatR;

namespace core.Account
{
    public class PasswordReset
    {
        public class Request : IRequest<PasswordResetResult>
        {
            [Required]
            public string Email { get; set; }
            public string IPAddress { get; private set; }

            public void WithIPAddress(string ipAddress)
            {
                this.IPAddress = ipAddress;
            }
        }

        public class Handler : IRequestHandler<Request, PasswordResetResult>
        {
            private IAccountStorage _storage;
            private IEmailService _emailService;

            public Handler(IAccountStorage storage, IEmailService emailService)
            {
                _storage = storage;
                _emailService = emailService;
            }

            public async Task<PasswordResetResult> Handle(Request request, CancellationToken cancellationToken)
            {
                var user = await this._storage.GetUserByEmail(request.Email);
                if (user == null)
                {
                    // not really success, but we are not going to disclose
                    // if user account exists for a given email
                    return PasswordResetResult.Success();
                }

                user.RequestPasswordReset(DateTimeOffset.UtcNow);

                await this._storage.Save(user);

                SendEmail(request, user);

                return PasswordResetResult.Success();
            }

            private void SendEmail(Request request, User user)
            {
                var reseturl = "https://www.graphdrive.com/profile/passwordreset/" + Guid.NewGuid();

                _emailService.Send(
                    request.Email,
                    EmailSettings.TemplatePasswordReset,
                    new {reseturl}
                );
            }
        }
    }
}