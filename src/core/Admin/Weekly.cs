using System;
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
        {}

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

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var users = await _storage.GetUserEmailIdPairs();

                foreach(var u in users)
                {
                    if (u.Item1 != "laimis@gmail.com")
                    {
                        Console.WriteLine("Skipping user " + u.Item1);
                        continue;
                    }

                    Console.WriteLine("Processing weekly review for " + u.Item1);

                    var review = new Review.Generate(DateTimeOffset.UtcNow);

                    review.WithUserId(new Guid(u.Item2));

                    var r = await _mediator.Send(review);

                    var portfolio = r.Entries.Where(e => e.Ownership.Count > 0).ToList();

                    if (portfolio.Count == 0)
                    {
                        Console.WriteLine("No portfolio items for " + u.Item1);
                        continue;
                    }

                    var data = new {
                        portfolio = portfolio.SelectMany(p => p.Ownership.Select(re => (p, re)))
                            .Select(pair => new {
                                ticker = pair.p.Ticker,
                                value = pair.p.Price.Amount,
                                description = pair.re.Description,
                                expiration = pair.re.Expiration.HasValue ? pair.re.Expiration.Value.ToString("MMM, dd") : null,
                                earnings = pair.p.EarningsWarning ? pair.p.EarningsDate.Value.ToString("MMM, dd") : null
                            }),
                        timestamp = review.Date.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                    };
                    
                    Console.WriteLine(JsonConvert.SerializeObject(data));

                    await _emails.Send(u.Item1, EmailSender.Support, EmailTemplate.ReviewEmail, data);
                }

                return new Unit();
            }
        }
    }
}