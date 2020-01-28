using System;
using core.Options;
using Xunit;

namespace coretests.Options
{
    public class SoldOptionTests
    {
        [Fact]
        public void PutOptionOperations()
        {
            var option = GetTestOption(OptionType.PUT);

            Assert.Equal("TEUM", option.State.Ticker);
            Assert.Equal("laimonas", option.State.UserId);
            Assert.Equal(2.5, option.State.StrikePrice);
            Assert.Equal(32, option.State.Premium);
            Assert.Equal(1, option.State.Amount);
            Assert.True(option.State.Expiration.Hour == 0);
            Assert.NotNull(option.State.Filled);
            Assert.Null(option.State.Closed);
            Assert.Equal(0, option.State.CollateralShares);
            Assert.Equal(218, option.State.CollateralCash);

            option.Close(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(0, option.State.Amount);
            Assert.NotNull(option.State.Closed);

            Assert.Equal(10, option.State.Spent);

            Assert.Equal(22, option.State.Profit);
        }

        [Fact]
        public void CallOptionOperations()
        {
            var option = GetTestOption(OptionType.CALL);

            Assert.Equal(100, option.State.CollateralShares);
            Assert.Equal(0, option.State.CollateralCash);
        }

        private static SoldOption GetTestOption(OptionType optionType)
        {
            var option = new SoldOption(
                "TEUM",
                optionType,
                DateTimeOffset.UtcNow.AddDays(10),
                2.5,
                "laimonas",
                1,
                32,
                DateTimeOffset.UtcNow);

            return option;
        }

        [Fact]
        public void CreateWithBadTickerFails()
        {
            Assert.Throws<InvalidOperationException>( () =>
                new SoldOption(null, OptionType.CALL, DateTimeOffset.UtcNow, 2, "user", 1, 20, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void CreateWithBadUserFails()
        {
            Assert.Throws<InvalidOperationException>( () =>
                new SoldOption("ticker", OptionType.CALL, DateTimeOffset.UtcNow, 2, "", 1, 20, DateTimeOffset.UtcNow));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void CreateWithBadStrikeFails(double strikePrice)
        {
            Assert.Throws<InvalidOperationException>( () =>
                new SoldOption("ticker", OptionType.CALL, DateTimeOffset.UtcNow, strikePrice, "user", 1, 20, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void CreateWithPastExpirationFails()
        {
            Assert.Throws<InvalidOperationException>( () =>
                new SoldOption("ticker", OptionType.CALL, DateTimeOffset.UtcNow.AddDays(-1), 2, "user", 1, 20, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void CreateWithFarFutureExpirationFails()
        {
            Assert.Throws<InvalidOperationException>( () =>
                new SoldOption("ticker", OptionType.CALL, DateTimeOffset.UtcNow.AddDays(700), 2, "user", 1, 20, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void EventCstrReplays()
        {
            var opt = GetTestOption(OptionType.CALL);

            var opt2 = new SoldOption(opt.Events);

            Assert.Equal(opt.State.Expiration, opt2.State.Expiration);
            Assert.Equal(opt.State.Key, opt2.State.Key);
            Assert.Equal(opt.State.Ticker, opt2.State.Ticker);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void OpenWithInvalidAmountFails(int amount)
        {
            Assert.Throws<InvalidOperationException>( () =>
                new SoldOption("ticker", OptionType.CALL, DateTimeOffset.UtcNow.AddDays(700), 2, "user", amount, 20, DateTimeOffset.UtcNow));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void OpenWithInvalidPremiumFails(int premium)
        {
            var opt = GetTestOption(OptionType.CALL);

            Assert.Throws<InvalidOperationException>( () =>
                new SoldOption("ticker", OptionType.CALL, DateTimeOffset.UtcNow.AddDays(700), 2, "user", 1, premium, DateTimeOffset.UtcNow));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void CloseWithInvalidAmountFails(int amount)
        {
            var opt = GetTestOption(OptionType.CALL);

            Assert.Throws<InvalidOperationException>( () =>
                opt.Close(amount, 0, DateTimeOffset.UtcNow));
        }

        [Theory]
        [InlineData(-1)]
        public void CloseWithInvalidMoneyFails(int money)
        {
            var opt = GetTestOption(OptionType.CALL);

            Assert.Throws<InvalidOperationException>( () =>
                opt.Close(1, money, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void CloseWithTooManyContractsFails()
        {
            var opt = GetTestOption(OptionType.CALL);

            Assert.Throws<InvalidOperationException>( () =>
                opt.Close(200, 0, DateTimeOffset.UtcNow));
        }
    }
}
