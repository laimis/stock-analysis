using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.CSV;
using MediatR;

namespace core.Cryptos.Handlers
{
    public partial class ImportCoinbasePro
    {
        public class Command : RequestWithUserId<CommandResponse>
        {
            public Command(string content)
            {
                Content = content;
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
                var parseResponse = _parser.Parse<CoinbaseProRecord>(request.Content);
                if (parseResponse.IsOk == false)
                {
                    return CommandResponse.Failed(parseResponse.Error!.Message);
                }

                try
                {
                    var coinbaseProContainer = new CoinbaseProContainer();
                    coinbaseProContainer.AddRecords(parseResponse.Success);

                    foreach(var b in coinbaseProContainer.GetBuys())
                    {
                        var cmd = new core.Cryptos.Handlers.Buy.Command {
                            Date = b.Date,
                            DollarAmount = b.DollarAmount,
                            Quantity = b.Quantity,
                            Token = b.Token
                        };
                        cmd.WithUserId(request.UserId);
                        await _mediator.Send(cmd, cancellationToken);
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

    public class CoinbaseProContainer
    {
        public List<TransactionGroup> Transactions { get; private set; }

        public void AddRecords(IEnumerable<CoinbaseProRecord> records)
        {
            Transactions = records
                .Where(r => r.Type == "match")
                .GroupBy(r => r.TradeId)
                .Select(g => new TransactionGroup(g))
                .ToList();
        }

        public IEnumerable<TransactionGroup> GetBuys() => Transactions.Where(t => t.IsBuy);
        public IEnumerable<TransactionGroup> GetSells() => Transactions.Where(t => t.IsSell);

        public class TransactionGroup
        {
            public TransactionGroup(IGrouping<string, CoinbaseProRecord> group)
            {
                var records = group.ToList();

                if (records.Count > 2)
                {
                    throw new InvalidOperationException("More than two records found for " + records[0].TradeId);
                }

                IsBuy = records[0].AmountBalanceUnit == "USD";
                
                DollarAmount = IsBuy switch {
                    true => Math.Abs(records[0].Amount.Value),
                    false => records[1].Amount.Value
                };

                Quantity = IsBuy switch {
                    true => records[1].Amount.Value,
                    false => Math.Abs(records[0].Amount.Value)
                };

                Token = IsBuy switch {
                    true => records[1].AmountBalanceUnit,
                    false => records[0].AmountBalanceUnit
                };

                Date = records[0].Time;
            }

            public bool IsBuy { get; }
            public bool IsSell => !IsBuy;
            public decimal DollarAmount { get; }
            public decimal Quantity { get; }
            public string Token { get; }
            public DateTimeOffset Date { get; }
        }
    }
}