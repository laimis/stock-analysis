using System;
using core.Shared.Adapters.Brokerage;
using TimeZoneConverter;

namespace web.Utils
{
    public class MarketHoursAlwaysOn : MarketHours
    {
        public new bool IsMarketOpen(DateTimeOffset time) => true;
    }

    public class MarketHours : IMarketHours
    {
        private static TimeZoneInfo _easternZoneId = TZConvert.GetTimeZoneInfo("Eastern Standard Time");
        private static TimeSpan StartTime = new TimeSpan(9, 30, 0);
        private static TimeSpan EndTime = new TimeSpan(16, 0, 0);

        public bool IsMarketOpen(DateTimeOffset utcNow)
        {
            // 930-1600
            var eastern = TimeZoneInfo.ConvertTimeFromUtc(
                utcNow.DateTime,
                _easternZoneId
            );

            if (eastern.DayOfWeek == DayOfWeek.Saturday || eastern.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }

            var timeOfDay = eastern.TimeOfDay;

            return timeOfDay >= StartTime && timeOfDay <= EndTime;
        }

        public DateTimeOffset ToMarketTime(DateTimeOffset when) => ConvertToEastern(when);

        public DateTimeOffset GetMarketStartOfDayTimeInUtc(DateTimeOffset eastern)
        {
            return TimeZoneInfo.ConvertTimeToUtc(
                eastern.Date.Add(StartTime),
                _easternZoneId
            );
        }

        public DateTimeOffset GetMarketEndOfDayTimeInUtc(DateTimeOffset when)
        {
            return TimeZoneInfo.ConvertTimeToUtc(
                when.Date.Add(EndTime),
                _easternZoneId
            );
        }

        public static DateTimeOffset ConvertToEastern(DateTimeOffset when)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                when.DateTime,
                _easternZoneId
            );
        }
    }
}