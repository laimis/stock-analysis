using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Emails;
using core.Adapters.Stocks;
using core.Alerts;
using core.Options;
using core.Stocks;
using MediatR;
using Newtonsoft.Json;

namespace core.Admin
{
    public class Weekly
    {
        public class Command : IRequest<List<object>>
        {
            [Required]
            public bool? Everyone { get; set; }
        }

        public class Handler : IRequestHandler<Command, List<object>>
        {
            private IAccountStorage _storage;
            private IAlertsStorage _alerts;
            private IStocksService2 _stocks;
            private IPortfolioStorage _portfolio;
            private IEmailService _emails;
            private IMediator _mediator;

            public Handler(
                IAlertsStorage alerts,
                IEmailService emails,
                IMediator mediator,
                IPortfolioStorage portfolio,
                IStocksService2 stocks,
                IAccountStorage storage
                )
            {
                _alerts = alerts;
                _emails = emails;
                _mediator = mediator;
                _portfolio = portfolio;
                _stocks = stocks;
                _storage = storage;
            }

            public async Task<List<object>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var users = await _storage.GetUserEmailIdPairs();

                var emailed = new List<object>();
                
                foreach(var u in users)
                {
                    if (!cmd.Everyone.Value && u.Item1 != "laimis@gmail.com")
                    {
                        Console.WriteLine("Skipping user " + u.Item1);
                        continue;
                    }

                    try
                    {
                        var result = await ProcessUser(u);

                        emailed.Add(new {
                            u.email,
                            result
                        });
                    }
                    catch(Exception error)
                    {
                        Console.Error.WriteLine(
                            "Failed to generate review for " + u.Item1 + " with error: " + error
                        );
                    }
                }

                return emailed;
            }

            private async Task<object> ProcessUser((string email, string id) u)
            {
                var userId = new Guid(u.id);

                var ownedOptions = _portfolio.GetOwnedOptions(userId);
                var stocks = _portfolio.GetStocks(userId);
                var alerts = _alerts.GetAlerts(userId);

                var date = DateTimeOffset.UtcNow;

                await Task.WhenAll(ownedOptions, stocks, alerts);

                var groups = await CreateReviewGroups(ownedOptions, stocks, alerts);

                var start = date.Date.AddDays(-7);
                var end = date.Date;

                var transactions = ownedOptions.Result.SelectMany(o => o.State.Transactions)
                    .Union(stocks.Result.SelectMany(s => s.State.Transactions))
                    .Where(t => t.DateAsDate >= start)
                    .Where(t => !t.IsPL);

                var r = new EmailReviewList(
                    start,
                    end,
                    groups,
                    new EmailTransactionList(transactions.Where(t => !t.IsOption), null, null),
                    new EmailTransactionList(transactions.Where(t => t.IsOption), null, null)
                );

                var portfolioEntries = r.Entries.Where(e => e.Ownership.Count > 0).ToList();
                var alertEntries = r.Entries.Where(e => e.Ownership.Count == 0 && e.Alerts.Count > 0).ToList();

                if (portfolioEntries.Count == 0 && alertEntries.Count == 0)
                {
                    return null;
                }

                var portfolio = portfolioEntries
                    .SelectMany(p => p.Ownership.Where(re => !re.IsOption)
                    .Select(re => (p, re)))
                    .Select(Map)
                    .OrderBy(e => e.ticker);

                var options = portfolioEntries
                    .SelectMany(p => p.Ownership.Where(re => re.IsOption)
                    .Select(re => (p, re)))
                    .Select(Map)
                    .OrderBy(e => e.ticker);

                var other = alertEntries
                    .Select(p => (p, p.Alerts.First()))
                    .Select(Map)
                    .OrderBy(e => e.ticker);

                var data = new
                {
                    portfolio,
                    options,
                    other,
                    timestamp = date.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                };

                await _emails.Send(
                    new Recipient(email: u.email, name: null),
                    Sender.Support,
                    EmailTemplate.ReviewEmail,
                    data
                );

                return data;
            }

            private async Task<List<EmailReviewEntryGroup>> CreateReviewGroups(
                Task<IEnumerable<OwnedOption>> options,
                Task<IEnumerable<OwnedStock>> stocks,
                Task<IEnumerable<Alert>> alerts)
            {
                var entries = new List<EmailReviewEntry>();

                foreach (var o in options.Result.Where(s => s.State.Active))
                {
                    entries.Add(new EmailReviewEntry(o));
                }

                foreach (var s in stocks.Result.Where(s => s.State.Owned > 0))
                {
                    entries.Add(new EmailReviewEntry(s));
                }

                foreach (var a in alerts.Result)
                {
                    if (a.PricePoints.Count > 0)
                    {
                        entries.Add(new EmailReviewEntry(a));
                    }
                }

                var grouped = entries.GroupBy(r => r.Ticker);
                var groups = new List<EmailReviewEntryGroup>();

                foreach (var group in grouped)
                {
                    var price = await _stocks.GetPrice(group.Key);
                    var advanced = await _stocks.GetAdvancedStats(group.Key);

                    groups.Add(new EmailReviewEntryGroup(group, price.Success, advanced.Success));
                }

                return groups;
            }

            private static WeeklyReviewEntryView Map((EmailReviewEntryGroup p, EmailReviewEntry re) pair)
            {
                return new WeeklyReviewEntryView(pair);
            }
        }
    }
}