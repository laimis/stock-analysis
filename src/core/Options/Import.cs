using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.CSV;
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
            private ICSVParser _parser;
            
            public Handler(IMediator mediator, ICSVParser parser)
            {
                _mediator = mediator;
                _parser = parser;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var records = _parser.Parse<OptionRecord>(request.Content);

                foreach(var r in records)
                {
                    await ProcessLine(r, request.UserId);
                }

                return new Unit();
            }

            private async Task ProcessLine(OptionRecord record, string userId)
            {
                var cmd = new Options.Sell.Command {
                    Amount = record.amount,
                    ExpirationDate = record.expiration,
                    Filled = record.filled,
                    OptionType = record.type,
                    Premium = record.premium,
                    StrikePrice = record.strike,
                    Ticker = record.ticker,
                };

                cmd.WithUserId(userId);

                var r = await _mediator.Send(cmd);

                if (record.closed != null)
                {
                    var c = new Options.Close.Command {
                        Amount = record.amount,
                        CloseDate = record.closed.Value,
                        ClosePrice = record.spent,
                        Id = r,
                    };

                    c.WithUserId(userId);

                    await _mediator.Send(c);
                }
            }

            private class OptionRecord
            {
                public DateTimeOffset? closed { get; set; }
                public DateTimeOffset? expiration { get; set; }
                public DateTimeOffset? filled { get; set; }
                public int amount { get; set; }
                public string type { get; set; }
                public double premium { get; internal set; }
                public double strike { get; internal set; }
                public string ticker { get; internal set; }
                public double? spent { get; internal set; }
            }
        }
    }
}