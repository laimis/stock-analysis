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
                var (records, err) = _parser.Parse<StockRecord>(request.Content);
                if (err != null)
                {
                    return CommandResponse.Failed(err);
                }
                
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
                RequestWithUserId<CommandResponse> CreateBuy(StockRecord r)
                {
                    var b = new core.Stocks.Buy.Command {
                        NumberOfShares = record.amount,
                        Date = record.date,
                        Price = record.price,
                        Ticker = record.ticker,
                    };
                    b.WithUserId(userId);
                    return b;
                }

                RequestWithUserId<CommandResponse> CreateSell(StockRecord r)
                {
                    var s = new core.Stocks.Sell.Command
                    {
                        NumberOfShares = record.amount,
                        Date = record.date,
                        Price = record.price,
                        Ticker = record.ticker,
                    };
                    s.WithUserId(userId);
                    return s;
                }

                RequestWithUserId<CommandResponse> cmd = null;
                switch (record.type)
                {
                    case "buy":
                        cmd = CreateBuy(record);
                        break;

                    case "sell":
                        cmd = CreateSell(record);
                        break;
                }

                try
                {
                    return await _mediator.Send(cmd);
                }
                catch(Exception ex)
                {
                    return CommandResponse.Failed(
                        $"Entry for {record.ticker}/{record.type}/{record.date.Value.ToString("yyyy-MM-dd")} failed: {ex.Message}"
                    );
                }
            }

            private class StockRecord
            {
                public int amount { get; set; }
                public string type { get; set; }
                public DateTimeOffset? date { get; set; }
                public decimal price { get; set; }
                public string ticker { get; set; }
            }
        }
    }
}