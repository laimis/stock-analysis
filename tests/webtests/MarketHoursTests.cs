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
                new MarketHours().IsOn(DateTimeOffset.Parse(time))
            );
        }
    }
}