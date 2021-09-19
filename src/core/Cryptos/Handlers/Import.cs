using System;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.CSV;
using core.Shared;
using MediatR;

namespace core.Cryptos.Handlers
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
                var (records, err) = _parser.Parse<CryptoRecord>(request.Content);
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

            private async Task<CommandResponse> ProcessLine(CryptoRecord record, Guid userId)
            {
                RequestWithUserId<CommandResponse> CreateBuy(CryptoRecord r)
                {
                    var b = new core.Cryptos.Handlers.Buy.Command {
                        Date = record.Timestamp,
                        DollarAmount = record.USDSubtotal,
                        Quantity = record.QuantityTransacted,
                        Token = record.Asset
                    };
                    b.WithUserId(userId);
                    return b;
                }

                RequestWithUserId<CommandResponse> CreateSell(CryptoRecord r)
                {
                    var s = new core.Cryptos.Handlers.Sell.Command
                    {
                        Date = record.Timestamp,
                        DollarAmount = record.USDSubtotal,
                        Quantity = record.QuantityTransacted,
                        Token = record.Asset
                    };
                    s.WithUserId(userId);
                    return s;
                }

                RequestWithUserId<CommandResponse> cmd = null;
                switch (record.TransactionType.ToLower())
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
                        $"Entry for {record.Asset}/{record.TransactionType}/{record.Timestamp.ToString("yyyy-MM-dd")} failed: {ex.Message}"
                    );
                }
            }

            private class CryptoRecord
            {
                public string TransactionType { get; set; }
                public string Asset { get; set; }
                public double QuantityTransacted { get; set; }
                public double USDSubtotal { get; set; }
                public DateTimeOffset Timestamp { get; set; }
            }
        }
    }
}