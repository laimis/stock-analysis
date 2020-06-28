using System;

namespace core.Utils
{
    public class MarketHours
    {

        private TimeZoneInfo _easternZoneId;

        public MarketHours()
        {
            try
            {
                _easternZoneId = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch(TimeZoneNotFoundException)
            {
                _easternZoneId = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            }
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