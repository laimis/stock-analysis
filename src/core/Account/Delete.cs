using System;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Emails;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class Delete
    {
        public class Command : RequestWithUserId
        {
            public string Feedback { get; set; }
        }

        public class Handler : MediatR.IRequestHandler<Command>
        {
            private IAccountStorage _storage;
            private IPortfolioStorage _portfolio;
            private IEmailService _emails;

            public Handler(
                IAccountStorage storage,
                IPortfolioStorage portfolioStorage,
                IEmailService emailService)
            {
                _storage = storage;
                _portfolio = portfolioStorage;
                _emails = emailService;
            }

            public async Task<Unit> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUser(cmd.UserId);
                if (user == null)
                {
                    return new Unit();
                }

                await _emails.Send(
                    EmailSettings.Admin,
                    EmailSettings.TemplateUserDeleted,
                    new {feedback = cmd.Feedback, email = user.State.Email});

                await _storage.Delete(user);

                await _portfolio.Delete(user.Id.ToString());
                
                return new Unit();
            }
        }
    }
}