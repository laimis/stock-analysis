using System;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.CSV;
using core.Shared;
using MediatR;

namespace core.Cryptos.Handlers
{
    public partial class ImportCoinbasePro
    {
        public class Command : RequestWithUserId<CommandResponse>
        {
            public Command(string content)
            {
                this.Content = content;
            }

            public string Content { get; }
        }

        public partial class Handler : IRequestHandler<Command, CommandResponse>
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
                var (records, err) = _parser.Parse<CoinbaseProRecord>(request.Content);
                if (err != null)
                {
                    return CommandResponse.Failed(err);
                }

                try
                {
                    var coinbaseProContainer = new CoinbaseProContainer();
                    coinbaseProContainer.AddRecords(records);

                    foreach(var b in coinbaseProContainer.GetBuys())
                    {
                        var cmd = new core.Cryptos.Handlers.Buy.Command {
                            Date = b.Date,
                            DollarAmount = b.DollarAmount,
                            Quantity = b.Quantity,
                            Token = b.Token
                        };
                        cmd.WithUserId(request.UserId);
                        await _mediator.Send(cmd);
                    }

                    foreach(var b in coinbaseProContainer.GetSells())
                    {
                        var cmd = new core.Cryptos.Handlers.Sell.Command {
                            Date = b.Date,
                            DollarAmount = b.DollarAmount,
                            Quantity = b.Quantity,
                            Token = b.Token
                        };
                        cmd.WithUserId(request.UserId);
                        await _mediator.Send(cmd);
                    }

                    return CommandResponse.Success();
                }
                catch(Exception ex)
                {
                    return CommandResponse.Failed(
                        $"Entry Failed: {ex.Message}"
                    );
                }
            }
        }
    }

    public class CoinbaseProRecord
    {
        public string Type { get; set; }
        public string AmountBalanceUnit { get; set; }
        public decimal? Amount { get; set; }
        public DateTimeOffset Time { get; set; }
        public string TradeId { get; set; }
        public string OrderId { get; set; }
    }
}