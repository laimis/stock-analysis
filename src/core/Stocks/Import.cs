using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Stocks
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
                var type = parts[1];
                var amount = Int32.Parse(parts[2]);
                var price = double.Parse(parts[3]);
                var date = DateTimeOffset.Parse(parts[4]);

                object cmd = null;
                switch (type)
                {
                    case "buy":
                        var b = new core.Stocks.Buy.Command
                        {
                            Amount = amount,
                            Date = date,
                            Price = price,
                            Ticker = ticker,
                        };

                        b.WithUserId(userId);
                        cmd = b;
                        break;

                    case "sell":
                        var s = new core.Stocks.Sell.Command
                        {
                            Amount = amount,
                            Date = date,
                            Price = price,
                            Ticker = ticker,
                        };

                        s.WithUserId(userId);
                        cmd = s;
                        break;
                }

                await _mediator.Send(cmd);
            }
        }
    }
}