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

            var timeOfDay = eastern.TimeOfDay;

            var start = new TimeSpan(9, 40, 0);
            var end = new TimeSpan(16, 0, 0);

            return timeOfDay >= start && timeOfDay <= end;
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