using core.Shared.Adapters.Brokerage;
using TimeZoneConverter;

namespace timezonesupport
{
    public class MarketHoursAlwaysOn : IMarketHours
    {
        private static readonly IMarketHours _marketHours = new MarketHours();

        DateTimeOffset IMarketHours.GetMarketEndOfDayTimeInUtc(DateTimeOffset when)
            => _marketHours.GetMarketEndOfDayTimeInUtc(when);

        DateTimeOffset IMarketHours.GetMarketStartOfDayTimeInUtc(DateTimeOffset when)
            => _marketHours.GetMarketStartOfDayTimeInUtc(when);

        bool IMarketHours.IsMarketOpen(DateTimeOffset time) => true;

        DateTimeOffset IMarketHours.ToMarketTime(DateTimeOffset utc)
            => _marketHours.ToMarketTime(utc);

        DateTimeOffset IMarketHours.ToUniversalTime(DateTimeOffset eastern)
            => _marketHours.ToUniversalTime(eastern);
    }

    public class MarketHours : IMarketHours
    {
        private static TimeZoneInfo _easternZoneId = TZConvert.GetTimeZoneInfo("Eastern Standard Time");
        private static TimeSpan StartTime = new TimeSpan(9, 30, 0);
        private static TimeSpan EndTime = new TimeSpan(16, 0, 0);

        private static DateTimeOffset ConvertToEastern(DateTimeOffset when)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                when.DateTime,
                _easternZoneId
            );
        }

        bool IMarketHours.IsMarketOpen(DateTimeOffset utc)
        {
            // 930-1600
            var eastern = TimeZoneInfo.ConvertTimeFromUtc(
                utc.DateTime,
                _easternZoneId
            );

            if (eastern.DayOfWeek == DayOfWeek.Saturday || eastern.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }

            var timeOfDay = eastern.TimeOfDay;

            return timeOfDay >= StartTime && timeOfDay <= EndTime;
        }

        DateTimeOffset IMarketHours.ToMarketTime(DateTimeOffset utc) => ConvertToEastern(utc);

        DateTimeOffset IMarketHours.ToUniversalTime(DateTimeOffset eastern) =>
            TimeZoneInfo.ConvertTimeToUtc(eastern.DateTime, _easternZoneId);

        DateTimeOffset IMarketHours.GetMarketEndOfDayTimeInUtc(DateTimeOffset eastern) =>
            TimeZoneInfo.ConvertTimeToUtc(
                eastern.Date.Add(EndTime),
                _easternZoneId
            );

        DateTimeOffset IMarketHours.GetMarketStartOfDayTimeInUtc(DateTimeOffset eastern) =>
            TimeZoneInfo.ConvertTimeToUtc(
                eastern.Date.Add(StartTime),
                _easternZoneId
            );
    }
}