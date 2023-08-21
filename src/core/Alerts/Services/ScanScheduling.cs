using System;
using System.Linq;
using core.Shared.Adapters.Brokerage;

namespace core.Alerts.Services
{
    public class ScanScheduling
    {
        private static TimeOnly[] _listMonitorTimes = new TimeOnly[]
            {
                TimeOnly.Parse("09:45"),
                TimeOnly.Parse("11:15"),
                TimeOnly.Parse("13:05"),
                TimeOnly.Parse("14:35"),
                TimeOnly.Parse("15:30")
            };

        public static DateTimeOffset GetNextListMonitorRunTime(
            DateTimeOffset referenceTimeUtc,
            IMarketHours marketHours
        )
        {
            var easternTime = marketHours.ToMarketTime(referenceTimeUtc);

            var candidates = _listMonitorTimes
                .Select(t => easternTime.Date.Add(t.ToTimeSpan()))
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

        
    }
}