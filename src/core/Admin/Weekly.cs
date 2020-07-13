using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Emails;
using core.Options;
using core.Portfolio;
using MediatR;
using Newtonsoft.Json;

namespace core.Admin
{
    public class Weekly
    {
        public class Command : IRequest
        {
            [Required]
            public bool? Everyone { get; set; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private IAccountStorage _storage;
            private IEmailService _emails;
            private IMediator _mediator;

            public Handler(
                IAccountStorage storage,
                IEmailService emails,
                IMediator mediator)
            {
                _storage = storage;
                _emails = emails;
                _mediator = mediator;
            }

            public async Task<Unit> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var users = await _storage.GetUserEmailIdPairs();

                foreach(var u in users)
                {
                    if (!cmd.Everyone.Value && u.Item1 != "laimis@gmail.com")
                    {
                        Console.WriteLine("Skipping user " + u.Item1);
                        continue;
                    }

                    try
                    {
                        await ProcessUser(u);
                    }
                    catch(Exception error)
                    {
                        Console.Error.WriteLine(
                            "Failed to generate review for " + u.Item1 + " with error: " + error
                        );
                    }
                }

                return new Unit();
            }

            private async Task ProcessUser((string email, string id) u)
            {
                var review = new Review.Generate(DateTimeOffset.UtcNow);

                review.WithUserId(new Guid(u.id));

                var r = await _mediator.Send(review);

                var portfolioEntries = r.Entries.Where(e => e.Ownership.Count > 0).ToList();
                var alertEntries = r.Entries.Where(e => e.Ownership.Count == 0 && e.Alerts.Count > 0).ToList();

                if (portfolioEntries.Count == 0 && alertEntries.Count == 0)
                {
                    return;
                }

                var portfolio = portfolioEntries
                    .SelectMany(p => p.Ownership.Where(re => !re.IsOption)
                    .Select(re => (p, re)))
                    .Select(Map)
                    .OrderBy(e => e.Ticker);

                var options = portfolioEntries
                    .SelectMany(p => p.Ownership.Where(re => re.IsOption)
                    .Select(re => (p, re)))
                    .Select(Map)
                    .OrderBy(e => e.Ticker);

                var other = alertEntries
                    .Select(p => (p, p.Alerts.First()))
                    .Select(Map)
                    .OrderBy(e => e.Ticker);

                var data = new
                {
                    portfolio,
                    options,
                    other,
                    timestamp = review.Date.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                };

                await _emails.Send(u.email, Sender.Support, EmailTemplate.ReviewEmail, data);
            }

            private static WeeklyReviewEntryView Map((ReviewEntryGroup p, ReviewEntry re) pair)
            {
                return new WeeklyReviewEntryView(pair);
            }
        }
    }
}