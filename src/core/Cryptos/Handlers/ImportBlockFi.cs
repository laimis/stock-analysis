using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.CSV;
using MediatR;

namespace core.Cryptos.Handlers
{
    public class ImportBlockFi
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
            private readonly IMediator _mediator;
            private readonly ICSVParser _parser;

            public Handler(IMediator mediator, ICSVParser parser)
            {
                _mediator = mediator;
                _parser = parser;
            }

            public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var parseResponse = _parser.Parse<BlockFiRecord>(request.Content);
                if (parseResponse.IsOk == false)
                {
                    return CommandResponse.Failed(parseResponse.Error!.Message);
                }

                foreach (var r in parseResponse.Success)
                {
                    var res = await ProcessLine(r, request.UserId);
                    if (res.Error != null)
                    {
                        return res;
                    }
                }

                return CommandResponse.Success();
            }

            private async Task<CommandResponse> ProcessLine(BlockFiRecord record, Guid userId)
            {
                RequestWithUserId<CommandResponse> CreateAward(BlockFiRecord r)
                {
                    var s = new core.Cryptos.Handlers.Reward.Command
                    {
                        Date = r.ConfirmedAt,
                        DollarAmount = 0,
                        Quantity = r.Amount,
                        Notes = r.Notes,
                        Token = r.Cryptocurrency
                    };
                    s.WithUserId(userId);
                    return s;
                }

                RequestWithUserId<CommandResponse> cmd = null;
                switch (record.TransactionType.ToLower())
                {
                    case "cc rewards redemption":
                        cmd = CreateAward(record);
                        break;

                    case "interest payment":
                        cmd = CreateAward(record);
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
                catch (Exception ex)
                {
                    return CommandResponse.Failed(
                        $"Entry for {record.Cryptocurrency}/{record.TransactionType}/{record.ConfirmedAt.ToString("yyyy-MM-dd")} failed: {ex.Message}"
                    );
                }
            }

            private class BlockFiRecord
            {
                public string TransactionType { get; set; }
                public string Cryptocurrency { get; set; }
                public decimal Amount { get; set; }
                public DateTimeOffset ConfirmedAt { get; set; }
                public string Notes => $"{TransactionType} of {Amount} {Cryptocurrency}";
            }
        }
    }
}