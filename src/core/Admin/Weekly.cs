using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Emails;
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

            private async Task ProcessUser((string, string) u)
            {
                Console.WriteLine("Processing weekly review for " + u.Item1);

                var review = new Review.Generate(DateTimeOffset.UtcNow);

                review.WithUserId(new Guid(u.Item2));

                var r = await _mediator.Send(review);

                var portfolioEntries = r.Entries.Where(e => e.Ownership.Count > 0).ToList();
                var notesEntries = r.Entries.Where(e => e.Ownership.Count == 0 && e.Notes.Count > 0).ToList();

                if (portfolioEntries.Count == 0 && notesEntries.Count == 0)
                {
                    Console.WriteLine("No portfolio or other items for " + u.Item1);
                    return;
                }

                Console.WriteLine("Owned: " + string.Join(",", portfolioEntries.Select(pe => pe.Ticker)));
                Console.WriteLine("Notes: " + string.Join(",", notesEntries.Select(pe => pe.Ticker)));

                var portfolio = portfolioEntries
                    .SelectMany(p => p.Ownership.Select(re => (p, re)))
                    .Select(Map);

                var other = notesEntries
                    .Where(p => p.EarningsWarning)
                    .Select(p => (p, p.Notes.First()))
                    .Select(Map);

                var data = new
                {
                    portfolio,
                    other,
                    timestamp = review.Date.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                };

                // Console.WriteLine(JsonConvert.SerializeObject(data));

                await _emails.Send(u.Item1, Sender.Support, EmailTemplate.ReviewEmail, data);
            }

            private static object Map((ReviewEntryGroup p, ReviewEntry re) pair)
            {
                return new
                {
                    ticker = pair.p.Ticker,
                    value = pair.p.Price.Amount,
                    description = pair.re.Description,
                    expiration = pair.re.Expiration.HasValue ? pair.re.Expiration.Value.ToString("MMM, dd") : null,
                    earnings = pair.p.EarningsWarning ? pair.p.EarningsDate.Value.ToString("MMM, dd") : null
                };
            }
        }
    }
}