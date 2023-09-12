using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Alerts;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Emails;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices;
public class EmailNotificationService : GenericBackgroundServiceHost
{
    private IAccountStorage _accounts;
    private StockAlertContainer _container;
    private IEmailService _emails;
    private IMarketHours _marketHours;
    Func<TimeSpan> _sleepFunction = null;

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

    private static TimeOnly[] _emailTimes = new TimeOnly[]
            {
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

            foreach (var (_, userId) in users)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                _logger.LogInformation($"Sending email to {userId}");

                var user = await _accounts.GetUser(new Guid(userId));
                if (user == null)
                {
                    _logger.LogError("Unable to find user for " + userId);
                    continue;
                }

                // get all alerts for that user
                var alertGroups = _container.GetAlerts(user.State.Id)
                    .GroupBy(a => a.identifier)
                    .Select(ToAlertEmailGroup);

                var data = new { alertGroups };

                await _emails.Send(
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
            alerts = group
                .OrderBy(a => a.sourceList)
                .ThenBy(a => a.ticker)
                .Select(ToEmailData)
        };
    }

    private object ToEmailData(TriggeredAlert alert)
    {
        var valueType = alert.valueType;
        var triggeredValue = alert.triggeredValue;
        var ticker = alert.ticker;
        var description = alert.description;
        var sourceList = alert.sourceList;
        var time = alert.when;

        return ToEmailRow(valueType, triggeredValue, ticker, description, sourceList, _marketHours.ToMarketTime(time));
    }

    public static object ToEmailRow(core.Shared.ValueFormat valueType, decimal triggeredValue, string ticker, string description, string sourceList, DateTimeOffset time)
    {
        string FormattedValue()
        {
            return valueType switch
            {
                core.Shared.ValueFormat.Percentage => triggeredValue.ToString("P1"),
                core.Shared.ValueFormat.Currency => triggeredValue.ToString("C2"),
                core.Shared.ValueFormat.Number => triggeredValue.ToString("N2"),
                core.Shared.ValueFormat.Boolean => triggeredValue.ToString(),
                _ => throw new Exception("Unexpected alert value type: " + valueType)
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
