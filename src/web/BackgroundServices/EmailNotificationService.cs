using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.fs.Alerts;
using core.fs.Shared.Adapters.Brokerage;
using core.fs.Shared.Adapters.Email;
using core.fs.Shared.Adapters.Storage;
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

                // get all alerts for that user
                var alertGroups = _container.GetAlerts(emailId.Id)
                    .GroupBy(a => a.identifier)
                    .Select(ToAlertEmailGroup);

                var data = new { alertGroups };

                await _emails.SendWithTemplate (
                    new Recipient(email: user.State.Email, name: user.State.Name),
                    Sender.NoReply,
                    EmailTemplate.Alerts,
                    data
                );

                _container.AddNotice("Emails sent");
            }

            _sleepFunction = NextStopLossCheck;
        }
    }

    private object ToAlertEmailGroup(IGrouping<string, TriggeredAlert> group)
    {
        return new {
            identifier = group.Key,
            alertCount = group.Count(), // need to include this as some template engines don't support length calls on collections
            alerts = group.Select(ToEmailData)
        };
    }

    private object ToEmailData(TriggeredAlert alert)
    {
        var valueFormat = alert.valueFormat;
        var triggeredValue = alert.triggeredValue;
        var ticker = alert.ticker;
        var description = alert.description;
        var sourceList = alert.sourceList;
        var time = alert.when;

        return ToEmailRow(valueFormat, triggeredValue, ticker, description, sourceList, _marketHours.ToMarketTime(time));
    }

    public static object ToEmailRow(string valueFormat, decimal triggeredValue, string ticker, string description, string sourceList, DateTimeOffset time)
    {
        string FormattedValue()
        {
            return valueFormat switch
            {
                core.Shared.ValueFormat.Percentage => triggeredValue.ToString("P1"),
                core.Shared.ValueFormat.Currency => triggeredValue.ToString("C2"),
                core.Shared.ValueFormat.Number => triggeredValue.ToString("N2"),
                core.Shared.ValueFormat.Boolean => triggeredValue.ToString(CultureInfo.InvariantCulture),
                _ => throw new Exception("Unexpected alert value type: " + valueFormat)
            };
        }

        return new
        {
            ticker,
            value = FormattedValue(),
            description,
            sourceList,
            time = time.ToString("HH:mm") + " ET"
        };
    }
}
