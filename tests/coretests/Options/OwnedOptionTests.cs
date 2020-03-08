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
            var option = GetTestOption(_expiration);

            option.Sell(1, 10, DateTimeOffset.UtcNow, "some notes");
            
            Assert.Equal(-1, option.State.NumberOfContracts);
            Assert.Single(option.State.Transactions);

            Assert.False(option.State.Transactions[0].IsPL);

            option.Buy(1, 1, DateTimeOffset.UtcNow, "some notes");

            Assert.Equal(0, option.State.NumberOfContracts);
            Assert.Equal(3, option.State.Transactions.Count);

            Assert.True(option.State.Transactions[2].IsPL);
            Assert.Equal(9, option.State.Transactions[2].Credit);
        }

        [Fact]
        public void Expire_CountsAsPLTransaction()
        {
            var option = GetTestOption(DateTimeOffset.UtcNow.AddDays(-1));

            option.Sell(1, 10, DateTimeOffset.UtcNow.AddDays(-2), "some notes");

            option.Expire(false);

            Assert.Equal(2, option.State.Transactions.Count);
            Assert.True(option.State.Transactions[1].IsPL);
            Assert.Equal(10, option.State.Transactions[1].Credit);
        }

        [Fact]
        public void ExpiresSoonWithin7Days()
        {
            var option = GetTestOption(DateTimeOffset.UtcNow.AddDays(2));

            Assert.True(option.ExpiresSoon);
            Assert.False(option.IsExpired);
        }

        [Fact]
        public void Expired()
        {
            var option = GetTestOption(DateTimeOffset.UtcNow);

            Assert.True(option.ExpiresSoon);
            Assert.False(option.IsExpired);

            option = GetTestOption(DateTimeOffset.UtcNow.AddDays(-1));

            Assert.False(option.ExpiresSoon);
            Assert.True(option.IsExpired);
        }

        [Fact]
        public void IsMatchWorks()
        {
            var option = GetTestOption(_expiration);

            option.IsMatch("TEUM", 2.5, OptionType.PUT, _expiration);
        }

        [Theory]
        [InlineData("tlsa",   -1)]
        [InlineData("tlsa",   0)]
        public void CreateWithBadTickerFails(string ticker, double strikePrice)
        {
            Assert.Throws<InvalidOperationException>( () =>
                new OwnedOption(ticker, strikePrice, OptionType.CALL, DateTimeOffset.UtcNow, Guid.NewGuid()));
        }

        [Fact]
        public void EventCstrReplays()
        {
            var opt = GetTestOption(_expiration);

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
        public void CloseWithInvalidInput(int numberOfContracts, double money)
        {
            var opt = GetTestOption(_expiration);

            Assert.Throws<InvalidOperationException>( () =>
                opt.Sell(numberOfContracts, money, DateTimeOffset.UtcNow, "some notes"));
        }

        private static OwnedOption GetTestOption(
            DateTimeOffset expiration,
            string ticker = "TEUM",
            OptionType optionType = OptionType.PUT,
            double strikePrice = 2.5)
        {
            var option = new OwnedOption(
                ticker,
                strikePrice,
                optionType,
                expiration,
                Guid.NewGuid());

            return option;
        }
    }
}
