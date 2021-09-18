using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Emails;
using core.Reports;
using core.Reports.Views;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class ThirtyDaySellService : BackgroundService
    {
        private IAccountStorage _accounts;
        private ILogger<ThirtyDaySellService> _logger;
        private IMediator _mediator;
        private IEmailService _emails;

        public ThirtyDaySellService(
            ILogger<ThirtyDaySellService> logger,
            IAccountStorage accounts,
            IEmailService emails,
            IMediator mediator)
        {
            _accounts = accounts;
            _emails = emails;
            _logger = logger;
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("thirty day monitor");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Loop(stoppingToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError("Failed:" + ex);
                }
            }

            _logger.LogInformation("thirty day monitor exit");
        }

        private async Task Loop(CancellationToken stoppingToken)
        {
            var time = DateTimeOffset.UtcNow;
            
            await ScanSells();

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }

        private async Task ScanSells()
        {
            var pairs = await _accounts.GetUserEmailIdPairs();

            foreach(var p in pairs)
            {
                // 30 day crosser
                var sellsOfInterest = new List<SellView>();
                var query = new Sells.Query(new Guid(p.id));

                var sellView = await _mediator.Send(query);

                foreach(var s in sellView.Sells)
                {
                    if (s.Date.Date == DateTime.UtcNow)
                    {
                        sellsOfInterest.Add(s);
                    }
                }

                await _emails.Send(
                    recipient: p.email,
                    Sender.NoReply,
                    template: EmailTemplate.SellAlert,
                    sellsOfInterest
                );
            }
        }
    }
}