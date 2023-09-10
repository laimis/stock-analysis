using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.CSV;
using core.Adapters.Emails;
using core.Options;
using core.Shared;
using core.Stocks;
using MediatR;

namespace core.Transactions.Handlers
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
            private IEmailService _emailService;
            private IMediator _mediator;
            private ICSVParser _parser;
            private IAccountStorage _storage;

            public Handler(
                IEmailService emailService,
                IMediator mediator,
                ICSVParser parser,
                IAccountStorage storage)
            {
                _emailService = emailService;
                _mediator = mediator;
                _parser = parser;
                _storage = storage;
            }

            public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var parseResponse = _parser.Parse<TransactionRecord>(request.Content);
                if (!parseResponse.IsOk)
                {
                    return CommandResponse.Failed(parseResponse.Error!.Message);
                }

                var user = await _storage.GetUser(request.UserId);
                if (user == null)
                {
                    return CommandResponse.Failed("User not found");
                }

                await SendEmail(user, subject: "Started importing transactions", body: "");

                var errors = new List<string>();
                try
                {
                    foreach (var cmd in GetCommands(parseResponse.Success))
                    {
                        cmd.WithUserId(request.UserId);
                        var response = await _mediator.Send(cmd, cancellationToken);
                        if (response is CommandResponse { Error: not null } r)
                        {
                            errors.Add(r.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await SendEmail(
                        user: user,
                        subject: "Failed to import transactions",
                        body: ex.ToString()
                    );

                    return CommandResponse.Failed($"Entry Failed: {ex}");
                }

                var onFinished = errors.Count switch
                {
                    0 => AfterSuccess(user),
                    _ => AfterFailure(errors, user)
                };

                return await onFinished;
            }

            private async Task<CommandResponse> AfterFailure(List<string> errors, User user)
            {
                var body = string.Join(",", errors);

                await _emailService.Send(
                    recipient: new Recipient(email: user.State.Email, name: user.State.Name),
                    sender: Sender.NoReply,
                    subject: "Failed to import transactions",
                    body: body
                );

                return CommandResponse.Failed(body);
            }

            private async Task<CommandResponse> AfterSuccess(User user)
            {
                await _emailService.Send(
                    recipient: new Recipient(email: user.State.Email, name: user.State.Name),
                    sender: Sender.NoReply,
                    subject: "Finished importing transactions",
                    body: ""
                );

                return CommandResponse.Success();
            }

            private Task SendEmail(User user, string subject, string body) =>
                _emailService.Send(
                    recipient: new Recipient(email: user.State.Email, name: user.State.Name),
                    sender: Sender.NoReply,
                    subject: subject,
                    body: body
                );

            private IEnumerable<RequestWithUserIdBase> GetCommands(IEnumerable<TransactionRecord> records)
            {
                // transaction records come in sorted by descending date
                foreach(var record in records.Reverse().Where(t => t.Qualify))
                {
                    if (record.IsStock())
                    {
                        yield return CreateStockTransaction(record);
                    }
                    else if (record.IsOption())
                    {
                        yield return CreateOptionTransaction(record);
                    }
                }
            }

            private StockTransaction CreateStockTransaction(TransactionRecord record) => record.IsBuy() switch
            {
                true => new core.Stocks.Buy.Command { Ticker = record.Symbol, Date = record.Date, Notes = record.Description, NumberOfShares = record.Quantity.Value, Price = record.Price.Value },
                false => new core.Stocks.Sell.Command { Ticker = record.Symbol, Date = record.Date, Notes = record.Description, NumberOfShares = record.Quantity.Value, Price = record.Price.Value }
            };

            private OptionTransaction CreateOptionTransaction(TransactionRecord record) => record.IsBuy() switch
            {
                true => new core.Options.Buy.Command { Ticker = record.GetTickerFromOptionDescription(), Filled = record.Date, Notes = record.Description, NumberOfContracts = Decimal.ToInt32(record.Quantity.Value), Premium = record.Price.Value * 100, StrikePrice = record.StrikePrice(), ExpirationDate = record.ExpirationDate(), OptionType = record.OptionType() },
                false => new core.Options.Sell.Command { Ticker = record.GetTickerFromOptionDescription(), Filled = record.Date, Notes = record.Description, NumberOfContracts = Decimal.ToInt32(record.Quantity.Value), Premium = record.Price.Value * 100, StrikePrice = record.StrikePrice(), ExpirationDate = record.ExpirationDate(), OptionType = record.OptionType() }
            };

            private class TransactionRecord
            {
                public DateTimeOffset Date { get; set; }
                public string TransactionId { get; set; }
                public string Description { get; set; }
                public decimal? Quantity { get; set; }
                public string Symbol { get; set; }
                public decimal? Price { get; set; }
                internal bool Qualify => IsBuy() || IsSell();
                internal bool IsBuy() => Description.StartsWith("Bought ");
                internal bool IsSell() => Description.StartsWith("Sold ");
                internal bool IsOption() => _optionContractRegex.IsMatch(Description);
                internal bool IsStock() => !IsOption();

                // LOB Nov 19 2021 60.0 Call
                private static readonly Regex _optionContractRegex = new Regex(@"(\w+) (\w+ \d+ \d+) (\d{1,6}.\d{1,4}) (Put|Call)");
                internal decimal StrikePrice()
                {
                    var match = _optionContractRegex.Match(Description);

                    return Decimal.Parse(match.Groups[3].Value);
                }

                internal DateTimeOffset? ExpirationDate()
                {
                    var match = _optionContractRegex.Match(Description);

                    return DateTimeOffset.ParseExact(match.Groups[2].Value, "MMM dd yyyy", null);
                }

                internal string OptionType()
                {
                    var match = _optionContractRegex.Match(Description);

                    return match.Groups[4].Value;
                }

                internal string GetTickerFromOptionDescription()
                {
                    var match = _optionContractRegex.Match(Description);

                    return match.Groups[1].Value;
                }
            }
        }
    }
}