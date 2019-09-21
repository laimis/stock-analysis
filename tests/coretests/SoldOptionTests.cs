using System;
using core.Portfolio;
using Xunit;

namespace coretests
{
    public class SoldOptionTests
    {
        [Fact]
        public void PurchaseWorks()
        {
            var option = new SoldOption(
                "TEUM",
                OptionType.PUT,
                DateTimeOffset.UtcNow.AddDays(10),
                2.5,
                "laimonas"
            );

            option.Open(1, 32, DateTimeOffset.UtcNow);

            Assert.Equal("TEUM", option.State.Ticker);
            Assert.Equal("laimonas", option.State.UserId);
            Assert.Equal(2.5, option.State.StrikePrice);
            Assert.Equal(32, option.State.Premium);
            Assert.Equal(1, option.State.Amount);
            Assert.True(option.State.Expiration.Hour == 0);

            option.Open(1, 40, DateTimeOffset.UtcNow);

            Assert.Equal(72, option.State.Premium);

            Assert.Equal(2, option.State.Amount);

            option.Close(1, 0, DateTimeOffset.UtcNow);

            Assert.Equal(1, option.State.Amount);
            
            option.Close(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(0, option.State.Amount);

            Assert.Equal(10, option.State.Spent);

            Assert.Equal(62, option.State.Profit);
        }
    }
}
