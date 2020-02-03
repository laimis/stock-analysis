using System;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.CSV;
using core.Shared;
using MediatR;

namespace core.Stocks
{
    public class Import
    {
        public class Command : RequestWithUserId<CommandResponse>
        {
            public Command(string content)
            {
                this.Content = content;
            }

            public string Content { get; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse>
        {
            private IMediator _mediator;
            private ICSVParser _parser;

            public Handler(IMediator mediator, ICSVParser parser)
            {
                _mediator = mediator;
                _parser = parser;
            }

            public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var records = _parser.Parse<StockRecord>(request.Content);
                foreach(var r in records)
                {
                    var res = await ProcessLine(r, request.UserId);
                    if (res.Error != null)
                    {
                        return res;
                    }
                }

                return CommandResponse.Success();
            }

            private async Task<CommandResponse> ProcessLine(StockRecord record, Guid userId)
            {
                RequestWithUserId<CommandResponse> cmd = null;
                switch (record.type)
                {
                    case "buy":
                        var b = new core.Stocks.Buy.Command
                        {
                            NumberOfShares = record.amount,
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
                            NumberOfShares = record.amount,
                            Date = record.date,
                            Price = record.price,
                            Ticker = record.ticker,
                        };

                        s.WithUserId(userId);
                        cmd = s;
                        break;
                }

                return await _mediator.Send(cmd);
            }

            private class StockRecord
            {
                public int amount { get; set; }
                public string type { get; set; }
                public DateTimeOffset? date { get; set; }
                public double price { get; set; }
                public string ticker { get; set; }
            }
        }
    }
}