using System;
using core.Options;
using Xunit;

namespace coretests.Options
{
    public class OwnedOptionTests
    {
        private static readonly DateTimeOffset _expiration = DateTimeOffset.UtcNow.AddDays(10);
        
        [Fact]
        public void PutOptionOperations()
        {
            var option = GetTestOption();

            option.Sell(1, 10, DateTimeOffset.UtcNow);
            
            Assert.Equal(-1, option.State.NumberOfContracts);
        }

        [Fact]
        public void IsMatchWorks()
        {
            var option = GetTestOption();

            option.IsMatch("TEUM", 2.5, OptionType.PUT, _expiration);
        }

        [Theory]
        [InlineData(null,       2,  "user")]
        [InlineData("ticker",   2,  "")]
        [InlineData("ticker",   -1,  "user")]
        [InlineData("ticker",   0,  "user")]
        public void CreateWithBadTickerFails(string ticker, double strikePrice, string userId)
        {
            Assert.Throws<InvalidOperationException>( () =>
                new OwnedOption(ticker, strikePrice, OptionType.CALL, DateTimeOffset.UtcNow, userId));
        }

        [Fact]
        public void EventCstrReplays()
        {
            var opt = GetTestOption();

            var opt2 = new OwnedOption(opt.Events);

            Assert.Equal(opt.State.Expiration, opt2.State.Expiration);
            Assert.Equal(opt.State.Id, opt2.State.Id);
            Assert.Equal(opt.State.Ticker, opt2.State.Ticker);
            Assert.Equal(opt.State.OptionType, opt.State.OptionType);
        }

        [Theory]
        [InlineData(-1, 0)] // negative contracts
        [InlineData(0, 0)]    // zero contracts
        [InlineData(1, -10)]    // negative money
        public void CloseWithInvalidAmountFails(int amount, double money)
        {
            var opt = GetTestOption();

            Assert.Throws<InvalidOperationException>( () =>
                opt.Sell(amount, money, DateTimeOffset.UtcNow));
        }

        private static OwnedOption GetTestOption(
            string ticker = "TEUM",
            OptionType optionType = OptionType.PUT,
            double strikePrice = 2.5,
            string userId = "testuser")
        {
            var option = new OwnedOption(
                ticker,
                strikePrice,
                optionType,
                _expiration,
                userId);

            return option;
        }
    }
}
