using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Emails;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class PasswordReset
    {
        public class Request : IRequest<CommandResponse>
        {
            [Required]
            public string Email { get; set; }
            public string IPAddress { get; private set; }

            public void WithIPAddress(string ipAddress)
            {
                IPAddress = ipAddress;
            }
        }

        public class Handler : IRequestHandler<Request, CommandResponse>
        {
            private IAccountStorage _storage;
            private IEmailService _emailService;

            public Handler(IAccountStorage storage, IEmailService emailService)
            {
                _storage = storage;
                _emailService = emailService;
            }

            public async Task<CommandResponse> Handle(Request request, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUserByEmail(request.Email);
                if (user == null)
                {
                    // not really success, but we are not going to disclose
                    // if user account exists for a given email
                    return CommandResponse.Success();
                }

                user.RequestPasswordReset(DateTimeOffset.UtcNow);

                await _storage.Save(user);

                return CommandResponse.Success();
            }
        }
    }
}