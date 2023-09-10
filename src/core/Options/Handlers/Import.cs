using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.CSV;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class Import
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
            private IPortfolioStorage _storage;

            public Handler(IMediator mediator, ICSVParser parser, IPortfolioStorage storage)
            {
                _mediator = mediator;
                _parser = parser;
                _storage = storage;
            }

            public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var parseResponse = _parser.Parse<OptionRecord>(request.Content);
                if (!parseResponse.IsOk)
                {
                    return CommandResponse.Failed(parseResponse.Error!.Message);
                }
                
                foreach(var r in parseResponse.Success)
                {
                    var response = await ProcessLine(r, request.UserId);
                    if (response.Error != null)
                    {
                        return response;
                    }
                }

                return CommandResponse.Success();
            }

            private async Task<CommandResponse> ProcessLine(OptionRecord record, Guid userId)
            {
                RequestWithUserIdBase cmd = record.type switch
                {
                    "sell" => new Sell.Command(),
                    "buy" => new Buy.Command(),
                    "expired" => new Expire.UnassignedCommand(),
                    "assigned" => new Expire.AssignedCommand(),
                    _ => throw new Exception($"Unexpected record type: {record.type}")
                };

                if (cmd is OptionTransaction ot)
                {
                    ot.ExpirationDate = record.expiration;
                    ot.Filled = record.filled;
                    ot.NumberOfContracts = record.amount;
                    ot.OptionType = record.optiontype;
                    ot.Premium = record.premium;
                    ot.StrikePrice = record.strike;
                    ot.Ticker = record.ticker;
                    ot.WithUserId(userId);

                    if (record.type == "buy")
                    {
                        ot.Premium = -1 * ot.Premium;
                    }

                    var r = await _mediator.Send(ot);
                    if (r.Error != null)
                    {
                        return r;
                    }
                }

                if (cmd is Expire.Command ec)
                {
                    var opts = await _storage.GetOwnedOptions(userId);

                    var optType = (OptionType)Enum.Parse(typeof(OptionType), record.optiontype);

                    var opt = opts.SingleOrDefault(o => 
                        o.IsMatch(record.ticker, record.strike, optType, record.expiration.Value)
                    );

                    if (opt == null)
                    {
                        return CommandResponse.Failed(
                            $"Unable to find option to expire for {record.ticker} {record.strike} {record.optiontype} {record.expiration}"
                        );
                    }

                    ec.Id = opt.Id;
                    ec.WithUserId(userId);

                    var er = await _mediator.Send(ec);

                    if (er.Error != null)
                    {
                        return er;
                    }
                }

                return CommandResponse.Success();
            }

            private class OptionRecord
            {
                public string ticker { get; set; }
                public string type { get; set; }
                public decimal strike { get; set; }
                public string optiontype { get; set; }
                public DateTimeOffset? expiration { get; set; }
                public int amount { get; set; }
                public decimal premium { get; set; }
                public DateTimeOffset? filled { get; set; }
            }
        }
    }
}