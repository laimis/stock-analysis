using System;
using System.Linq;
using core.Shared.Adapters.Brokerage;

namespace core.Alerts.Services
{
    public class ScanScheduling
    {
        private static TimeSpan[] _listMonitorTimes = new TimeSpan[]
            {
                TimeSpan.Parse("09:45"),
                TimeSpan.Parse("11:15"),
                TimeSpan.Parse("13:05"),
                TimeSpan.Parse("14:35"),
                TimeSpan.Parse("15:30")
            };

        private static TimeSpan[] _emailTimes = new TimeSpan[]
            {
                TimeSpan.Parse("09:50"),
                TimeSpan.Parse("15:45")
            };


        public static DateTimeOffset GetNextListMonitorRunTime(
            DateTimeOffset referenceTimeUtc,
            IMarketHours marketHours
        )
        {
            var easternTime = marketHours.ToMarketTime(referenceTimeUtc);

            var candidates = _listMonitorTimes
                .Select(t => easternTime.Date.Add(t))
                .ToArray();

            foreach(var candidate in candidates)
            {
                if (candidate > easternTime)
                {
                    return marketHours.ToUniversalTime(candidate);
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
            
            return marketHours.ToUniversalTime(nextDay);
        }

        // stop loss should be monitored at the following times:
        // on trading days every 5 minutes from 9:45am to 3:30pm
        // and no monitoring on weekends

        private static readonly TimeSpan _marketStartTime = new TimeSpan(9, 30, 0);
        private static readonly TimeSpan _marketEndTime = new TimeSpan(16, 0, 0);
        public static DateTimeOffset GetNextStopLossMonitorRunTime(DateTimeOffset time, IMarketHours marketHours)
        {
            var eastern = marketHours.ToMarketTime(time);
            var marketStartTimeInEastern = eastern.Date.Add(_marketStartTime);

            var nextScan = eastern.TimeOfDay switch {
                var t when t < _marketStartTime => marketStartTimeInEastern,
                var t when t > _marketEndTime => marketStartTimeInEastern.AddDays(1),
                _ => eastern.AddMinutes(5)
            };

            // if the next scan is on a weekend, let's skip those days
            if (nextScan.DayOfWeek == DayOfWeek.Saturday)
            {
                nextScan = nextScan.AddDays(2);
            }
            else if (nextScan.DayOfWeek == DayOfWeek.Sunday)
            {
                nextScan = nextScan.AddDays(1);
            }

            return marketHours.ToUniversalTime(nextScan);
        }

        // email should be sent at the following times:
        // on trading days at 9:50am, 3:45pm eastern
        // and no emails on weekends
        public static DateTimeOffset GetNextEmailRunTime(DateTimeOffset time, IMarketHours marketHours)
        {
            var eastern = marketHours.ToMarketTime(time);
            
            var candidates = _emailTimes
                .Select(t => eastern.Date.Add(t))
                .ToArray();

            foreach(var candidate in candidates)
            {
                if (candidate > eastern)
                {
                    return marketHours.ToUniversalTime(candidate);
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
            
            return marketHours.ToUniversalTime(nextDay);
        }
    }
}