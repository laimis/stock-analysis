using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.CSV;
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
            private ICSVParser _parser;

            public Handler(IMediator mediator, ICSVParser parser)
            {
                _mediator = mediator;
                _parser = parser;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var records = _parser.Parse<StockRecord>(request.Content);
                foreach(var r in records)
                    await ProcessLine(r, request.UserId);

                return new Unit();
            }

            private async Task ProcessLine(StockRecord record, string userId)
            {
                object cmd = null;
                switch (record.type)
                {
                    case "buy":
                        var b = new core.Stocks.Buy.Command
                        {
                            Amount = record.amount,
                            Date = record.date,
                            Price = record.price,
                            Ticker = record.ticker,
                        };

                        b.WithUserId(userId);
                        cmd = b;
                        break;

                    case "sell":
                        var s = new core.Stocks.Sell.Command
                        {
                            Amount = record.amount,
                            Date = record.date,
                            Price = record.price,
                            Ticker = record.ticker,
                        };

                        s.WithUserId(userId);
                        cmd = s;
                        break;
                }

                await _mediator.Send(cmd);
            }

            private class StockRecord
            {
                public int amount { get; set; }
                public string type { get; set; }
                public DateTimeOffset? date { get; internal set; }
                public double price { get; internal set; }
                public string ticker { get; internal set; }
            }
        }
    }
}