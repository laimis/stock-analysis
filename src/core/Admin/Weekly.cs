using System;
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
                        continue;
                    }

                    var review = new Review.Generate(DateTimeOffset.UtcNow);
                    review.WithUserId(new Guid(u.Item2));

                    var r = await _mediator.Send(review);

                    await _emails.Send(u.Item1, EmailSettings.TemplateReviewEmail, new {
                        content = JsonConvert.SerializeObject(r)
                    });
                }

                return new Unit();
            }
        }
    }
}