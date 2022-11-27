using System;
using web.Utils;
using Xunit;

namespace webtests
{
    public class MarketHoursTests
    {
        [Theory]
        [InlineData("2020-06-29T14:45:00Z", true)]
        [InlineData("2020-06-29T13:40:00Z", true)]
        [InlineData("2020-06-29T12:40:00Z", false)]
        public void IsOn(string time, bool isActiveMarket)
        {
            Assert.Equal(
                isActiveMarket,
                new MarketHours().IsMarketOpen(DateTimeOffset.Parse(time))
            );
        }

        [Fact]
        public void GetEndOfDayUtcAlwaysTheSameForThatDay()
        {
            var time = DateTime.UtcNow;

            var marketHours = new MarketHours();

            var endOfDay = marketHours.GetMarketEndOfDayTimeInUtc(time);

            Assert.Equal(
                time.ToString("yyyy-MM-dd 21:00:00"),
                endOfDay.ToString("yyyy-MM-dd HH:mm:ss")
            );
        }

    }
}