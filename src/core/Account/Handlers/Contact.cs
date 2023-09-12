using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.Emails;
using MediatR;

namespace core.Account.Handlers
{
    public class Contact
    {
        public class Command : RequestWithUserId
        {
            [Required]
            public string Email { get; set; }
            [Required]
            public string Message { get; set; }
        }

        public class Handler : MediatR.IRequestHandler<Command>
        {
            private IEmailService _emails;

            public Handler(IEmailService emailService)
            {
                _emails = emailService;
            }

            public async Task<Unit> Handle(Command cmd, CancellationToken cancellationToken)
            {
                await _emails.Send(
                    EmailSettings.Admin,
                    Sender.NoReply,
                    EmailTemplate.AdminContact,
                    new {message = cmd.Message, email = cmd.Email});

                return new Unit();
            }
        }
    }
}