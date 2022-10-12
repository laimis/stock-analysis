using System;
using TimeZoneConverter;

namespace web.Utils
{
    public interface IMarketHours
    {
        bool IsOn(DateTimeOffset time);
        DateTimeOffset ToMarketTime(DateTimeOffset when);
    }

    public class MarketHoursAlwaysOn : IMarketHours
    {
        public bool IsOn(DateTimeOffset time) => true;
        public DateTimeOffset ToMarketTime(DateTimeOffset when) => MarketHours.ConvertToEastern(when);
    }

    public class MarketHours : IMarketHours
    {
        private static TimeZoneInfo _easternZoneId = TZConvert.GetTimeZoneInfo("Eastern Standard Time");
        private TimeSpan _start = new TimeSpan(9, 40, 0);
        private TimeSpan _end = new TimeSpan(16, 0, 0);

        public bool IsOn(DateTimeOffset now)
        {
            if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }
            
            // 930-1600
            var eastern = TimeZoneInfo.ConvertTimeFromUtc(
                now.DateTime,
                _easternZoneId
            );

            var timeOfDay = eastern.TimeOfDay;

            return timeOfDay >= _start && timeOfDay <= _end;
        }

        public DateTimeOffset ToMarketTime(DateTimeOffset when) => ConvertToEastern(when);

        public static DateTimeOffset ConvertToEastern(DateTimeOffset when)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                when.DateTime,
                _easternZoneId
            );
        }
    }
}