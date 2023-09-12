using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.Emails;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Account.Handlers
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

                user.Delete(cmd.Feedback);

                await _storage.Save(user);

                await _emails.Send(
                    EmailSettings.Admin,
                    Sender.NoReply,
                    EmailTemplate.AdminUserDeleted,
                    new {feedback = cmd.Feedback, email = user.State.Email});

                await _storage.Delete(user);

                await _portfolio.Delete(user.Id);
                
                return new Unit();
            }
        }
    }
}