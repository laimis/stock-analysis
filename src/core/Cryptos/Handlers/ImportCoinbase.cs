using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.CSV;
using MediatR;

namespace core.Cryptos.Handlers
{
    public class ImportCoinbase
    {
        public class Command : RequestWithUserId<CommandResponse>
        {
            public Command(string content)
            {
                Content = content;
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
                var parseResponse = _parser.Parse<CryptoRecord>(request.Content);
                if (parseResponse.IsOk == false)
                {
                    return CommandResponse.Failed(parseResponse.Error!.Message);
                }
                
                foreach(var r in parseResponse.Success)
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
                        DollarAmount = record.USDSubtotal.Value,
                        Quantity = record.QuantityTransacted.Value,
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
                        DollarAmount = record.USDSubtotal.Value,
                        Quantity = record.QuantityTransacted.Value,
                        Token = record.Asset
                    };
                    s.WithUserId(userId);
                    return s;
                }

                RequestWithUserId<CommandResponse> CreateAward(CryptoRecord r)
                {
                    var s = new core.Cryptos.Handlers.Reward.Command
                    {
                        Date = record.Timestamp,
                        DollarAmount = record.USDSubtotal.Value,
                        Quantity = record.QuantityTransacted.Value,
                        Notes = record.Notes,
                        Token = record.Asset
                    };
                    s.WithUserId(userId);
                    return s;
                }

                RequestWithUserId<CommandResponse> CreateYield(CryptoRecord r)
                {
                    var s = new core.Cryptos.Handlers.Yield.Command
                    {
                        Date = record.Timestamp,
                        DollarAmount = record.USDSubtotal.Value,
                        Quantity = record.QuantityTransacted.Value,
                        Notes = record.Notes,
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

                    case "coinbase earn":
                        cmd = CreateAward(record);
                        break;

                    case "rewards income":
                        cmd = CreateYield(record);
                        break;
                }

                try
                {
                    if (cmd != null)
                    {
                        return await _mediator.Send(cmd);
                    }
                    else
                    {
                        return CommandResponse.Success();
                    }
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
                public decimal? QuantityTransacted { get; set; }
                public decimal? USDSubtotal { get; set; }
                public DateTimeOffset Timestamp { get; set; }
                public string Notes { get; set; }
            }
        }
    }
}