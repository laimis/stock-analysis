using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Reports;
using core.Reports.Views;
using core.Shared.Adapters.Emails;
using MediatR;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class ThirtyDaySellService : GenericBackgroundServiceHost
    {
        private readonly IAccountStorage _accounts;
        private readonly IMediator _mediator;
        private readonly IEmailService _emails;

        public ThirtyDaySellService(
            ILogger<ThirtyDaySellService> logger,
            IAccountStorage accounts,
            IEmailService emails,
            IMediator mediator) : base(logger)
        {
            _accounts = accounts;
            _emails = emails;
            _mediator = mediator;
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
                    _logger.LogError("Failed to process 30 day check for {email}: {exception}", p.email, ex);
                }
            }
        }

        private async Task ProcessUser((string email, string id) p)
        {
            // 30 day crosser
            _logger.LogInformation("Scanning {email}", p.email);

            var sellsOfInterest = new List<SellView>();
            var query = new Sells.Query(new Guid(p.id));

            var sellView = await _mediator.Send(query);

            foreach (var s in sellView.Sells)
            {
                if (s.NumberOfDays >= 27 && s.NumberOfDays <= 31)
                {
                    sellsOfInterest.Add(s);
                }
            }

            if (sellsOfInterest.Count > 0)
            {
                await _emails.Send(
                    recipient: new Recipient(email: p.email, name: null),
                    Sender.NoReply,
                    template: EmailTemplate.SellAlert,
                    new { sells = sellsOfInterest }
                );
            }
            else
            {
                _logger.LogInformation("No sells of interest for {email}", p.email);
            }
        }
    }
}