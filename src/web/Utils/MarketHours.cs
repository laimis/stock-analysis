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
        private TimeSpan _start = new TimeSpan(9, 40, 0);
        private TimeSpan _end = new TimeSpan(16, 0, 0);

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

            return timeOfDay >= _start && timeOfDay <= _end;
        }

        public DateTimeOffset ToMarketTime(DateTimeOffset when) => ConvertToEastern(when);

        public DateTimeOffset GetMarketStartOfDayTimeInUtc(DateTimeOffset when)
        {
            return TimeZoneInfo.ConvertTimeToUtc(
                when.Date.Add(_start),
                _easternZoneId
            );
        }

        public DateTimeOffset GetMarketEndOfDayTimeInUtc(DateTimeOffset when)
        {
            return TimeZoneInfo.ConvertTimeToUtc(
                when.Date.Add(_end),
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