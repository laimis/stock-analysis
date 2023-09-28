using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.fs.Reports;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
using core.Shared.Adapters.Emails;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class ThirtyDaySellService : GenericBackgroundServiceHost
    {
        private readonly IAccountStorage _accounts;
        private readonly Handler _service;
        private readonly IEmailService _emails;

        public ThirtyDaySellService(
            ILogger<ThirtyDaySellService> logger,
            IAccountStorage accounts,
            IEmailService emails,
            Handler service) : base(logger)
        {
            _accounts = accounts;
            _emails = emails;
            _service = service;
        }

        private static readonly TimeSpan _sleepInterval = TimeSpan.FromHours(24);
        protected override TimeSpan GetSleepDuration() => _sleepInterval;

        protected override async Task Loop(CancellationToken stoppingToken)
        {
            var pairs = await _accounts.GetUserEmailIdPairs();

            foreach(var p in pairs)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await ProcessUser(p);
                }
                catch(Exception ex)
                {
                    _logger.LogError("Failed to process 30 day check for {email}: {exception}", p.Email, ex);
                }
            }
        }

        private async Task ProcessUser(EmailIdPair p)
        {
            // 30 day crosser
            _logger.LogInformation("Scanning {email}", p.Email);

            var query = new SellsQuery(new Guid(p.Id));

            var sellView = await _service.Handle(query);

            if (sellView.IsOk == false)
            {
                _logger.LogError("Failed to get sells for {email}: {error}", p.Email, sellView.Error);
                return;
            }
            
            var sellsOfInterest = sellView.Success.Sells.Where(s => s.Age.Days is >= 27 and <= 31).ToList();

            if (sellsOfInterest.Count > 0)
            {
                await _emails.Send(
                    recipient: new Recipient(email: p.Email, name: null),
                    Sender.NoReply,
                    template: EmailTemplate.SellAlert,
                    new { sells = sellsOfInterest }
                );
            }
            else
            {
                _logger.LogInformation("No sells of interest for {email}", p.Email);
            }
        }
    }
}