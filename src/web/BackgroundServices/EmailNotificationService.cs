using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.fs;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.Email;
using core.fs.Adapters.Storage;
using core.fs.Alerts;
using core.Shared;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices;
public class EmailNotificationService : GenericBackgroundServiceHost
{
    private readonly IAccountStorage _accounts;
    private readonly StockAlertContainer _container;
    private readonly IEmailService _emails;
    private readonly IMarketHours _marketHours;
    Func<TimeSpan> _sleepFunction;

    public EmailNotificationService(
        IAccountStorage accounts,
        StockAlertContainer container,
        IEmailService emails,
        ILogger<EmailNotificationService> logger,
        IMarketHours marketHours) : base(logger)
    {
        _accounts = accounts;
        _container = container;
        _emails = emails;
        _marketHours = marketHours;
        _sleepFunction = NextStopLossCheck;
    }

    protected override TimeSpan GetSleepDuration() => _sleepFunction();

    private static readonly TimeOnly[] _emailTimes = {
                TimeOnly.Parse("09:50"),
                TimeOnly.Parse("15:45")
            };

    private TimeSpan NextStopLossCheck()
    {
        DateTimeOffset NextRun(DateTimeOffset now)
        {
            var eastern = _marketHours.ToMarketTime(now);
            
            var candidates = _emailTimes
                .Select(t => eastern.Date.Add(t.ToTimeSpan()))
                .ToArray();

            foreach(var candidate in candidates)
            {
                if (candidate > eastern)
                {
                    return _marketHours.ToUniversalTime(candidate);
                }
            }

            // if we get here, we need to look at the next day
            var nextDay = candidates[0].AddDays(1);

            // and if the next day is weekend, let's skip those days
            if (nextDay.DayOfWeek == DayOfWeek.Saturday)
            {
                nextDay = nextDay.AddDays(2);
            }
            else if (nextDay.DayOfWeek == DayOfWeek.Sunday)
            {
                nextDay = nextDay.AddDays(1);
            }
            
            return _marketHours.ToUniversalTime(nextDay);
        }
        
        var now = DateTimeOffset.UtcNow;
        
        var nextRun = NextRun(now);

        return nextRun - now;
    }

    private TimeSpan DelayToWaitForContainer() => TimeSpan.FromSeconds(30);

    protected override async Task Loop(CancellationToken stoppingToken)
    {
        if (!_container.ContainerReadyForNotifications())
        {
            _logger.LogInformation("Container not ready for notifications");
            _sleepFunction = DelayToWaitForContainer;
        }
        else
        {
            _logger.LogInformation("Container ready for notifications");
            
            var users = await _accounts.GetUserEmailIdPairs();

            foreach (var emailId in users)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                _logger.LogInformation($"Sending email to {emailId.Id}");

                var userOption = await _accounts.GetUser(emailId.Id);
                if (userOption == null)
                {
                    _logger.LogError("Unable to find user for " + emailId.Id);
                    continue;
                }

                var user = userOption.Value;

                var recipient = new Recipient(email: user.State.Email, name: user.State.Name);

                var alertGroups = _container.GetAlerts(emailId.Id)
                    .GroupBy(a => a.identifier);
                
                await SendAlerts(_emails, _marketHours, recipient, alertGroups);
            }
            
            _container.AddNotice("Emails sent");

            _sleepFunction = NextStopLossCheck;
        }
    }

    private static object ToAlertEmailGroup(IGrouping<string, TriggeredAlert> group, IMarketHours marketHours)
    {
        return new {
            identifier = group.Key,
            alertCount = group.Count(), // need to include this as some template engines don't support length calls on collections
            alerts = group.Select(g => ToEmailData(g, marketHours))
        };
    }

    private static object ToEmailData(TriggeredAlert alert, IMarketHours marketHours)
    {
        var valueFormat = alert.valueFormat;
        var triggeredValue = alert.triggeredValue;
        var ticker = alert.ticker;
        var description = alert.description;
        var sourceList = alert.sourceList;
        var time = alert.when;

        return ToEmailRow(valueFormat, triggeredValue, ticker, description, sourceList, marketHours.ToMarketTime(time));
    }

    private static object ToEmailRow(ValueFormat valueFormat, decimal triggeredValue, Ticker ticker, string description, string sourceList, DateTimeOffset time)
    {
        string FormattedValue()
        {
            return valueFormat.Tag switch
            {
                ValueFormat.Tags.Percentage => triggeredValue.ToString("P1"),
                ValueFormat.Tags.Currency => triggeredValue.ToString("C2"),
                ValueFormat.Tags.Number => triggeredValue.ToString("N2"),
                ValueFormat.Tags.Boolean => triggeredValue.ToString(CultureInfo.InvariantCulture),
                _ => throw new Exception("Unexpected alert value type: " + valueFormat)
            };
        }

        return new
        {
            ticker = ticker.Value,
            value = FormattedValue(),
            description,
            sourceList,
            time = time.ToString("HH:mm") + " ET"
        };
    }

    public static Task SendAlerts(IEmailService emails, IMarketHours marketHours, Recipient recipient, IEnumerable<IGrouping<string, TriggeredAlert>> grouping)
    {
        var alertGroups = grouping.Select(g => EmailNotificationService.ToAlertEmailGroup(g, marketHours));
            
        var data = new { alertGroups };

        return emails.SendWithTemplate(
            recipient: recipient,
            Sender.NoReply,
            template: EmailTemplate.Alerts,
            data
        );
    }
}
