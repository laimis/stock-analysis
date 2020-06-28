using System;
using TimeZoneConverter;

namespace core.Utils
{
    public class MarketHours
    {
        private TimeZoneInfo _easternZoneId;

        public MarketHours()
        {
            _easternZoneId = TZConvert.GetTimeZoneInfo("Eastern Standard Time");
        }

        public bool IsOn(DateTimeOffset offset)
        {
            // 930-1600
            var eastern = TimeZoneInfo.ConvertTimeFromUtc(
                offset.DateTime,
                _easternZoneId
            );

            return (eastern.Hour > 9 && eastern.Minute >= 30) && (eastern.Hour < 17 && eastern.Minute <= 0);
        }

        public DateTime ToMarketTime(DateTimeOffset when)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                when.DateTime,
                _easternZoneId
            );
        }
    }
}