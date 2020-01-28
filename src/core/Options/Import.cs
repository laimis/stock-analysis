using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class Import
    {
        public class Command : RequestWithUserId
        {
            public Command(string content)
            {
                this.Content = content;
            }

            public string Content { get; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private IMediator _mediator;

            public Handler(IMediator mediator)
            {
                _mediator = mediator;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                using var stringReader = new StringReader(request.Content);
                while(true)
                {
                    var line = await stringReader.ReadLineAsync();
                    if (line == null)
                    {
                        break;
                    }

                    if (line.StartsWith("ticker,"))
                    {
                        continue;
                    }

                    Console.WriteLine("processing " + line);
                    
                    var parts = line.Split(',');

                    await ProcessLine(parts, request.UserId);
                }

                return new Unit();
            }

            private async Task ProcessLine(string[] parts, string userId)
            {
                var ticker = parts[0];
                var strike = double.Parse(parts[1]);
                var type = parts[2];
                var expiration = DateTimeOffset.Parse(parts[3]);
                var filled = DateTimeOffset.Parse(parts[4]);
                var amount = Int32.Parse(parts[5]);
                if (amount == 0)
                {
                    amount = 1;
                }
                var premium = double.Parse(parts[6]);
                DateTimeOffset? closed = null;
                var closedString = parts[7];
                if (!string.IsNullOrEmpty(closedString))
                {
                    closed = DateTimeOffset.Parse(closedString);
                }
                var spent = double.Parse(parts[8]);

                var cmd = new Options.Sell.Command {
                    Amount = amount,
                    ExpirationDate = expiration,
                    Filled = filled,
                    OptionType = type,
                    Premium = premium,
                    StrikePrice = strike,
                    Ticker = ticker,
                };
                cmd.WithUserId(userId);

                var r = await _mediator.Send(cmd);

                if (closed != null)
                {
                    var c = new Options.Close.Command {
                        Amount = amount,
                        CloseDate = closed.Value,
                        ClosePrice = spent,
                        Id = r,
                    };

                    c.WithUserId(userId);

                    await _mediator.Send(c);
                }
            }
        }
    }
}