using System;

namespace core.Utils
{
    public static class MarketHours
    {
        private static TimeZoneInfo _easternZoneId = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        public static bool IsOn(DateTimeOffset offset)
        {
            // 930-1600
            var eastern = TimeZoneInfo.ConvertTimeFromUtc(
                offset.DateTime,
                _easternZoneId
            );

            return (eastern.Hour > 9 && eastern.Minute >= 30) && (eastern.Hour < 17 && eastern.Minute <= 0);
        }

        public static DateTime ToMarketTime(DateTimeOffset when)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                when.DateTime,
                _easternZoneId
            );
        }
    }
}