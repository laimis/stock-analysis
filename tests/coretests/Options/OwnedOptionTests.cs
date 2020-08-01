using System;
using System.Linq;
using core.Options;
using core.Shared;
using Xunit;

namespace coretests.Options
{
    public class OwnedOptionTests
    {
        private static readonly DateTimeOffset _expiration = DateTimeOffset.UtcNow.AddDays(10);

        [Fact]
        public void AssignedCallBug()
        {
            var date = new DateTimeOffset(2020, 3, 13, 0, 0, 0, 0, TimeSpan.FromHours(0));
            var user = Guid.NewGuid();

            var option = new OwnedOption(new Ticker("SFIX"), 17.5, OptionType.CALL, date, user);

            option.Sell(3, 100, date.AddDays(-20), null);
            option.Buy(2, 10, date.AddDays(-10), null);

            option.Expire(true);

            Assert.True(option.IsExpired);
            Assert.True(option.State.Assigned);
            Assert.False(option.IsActive);
            Assert.True(option.State.SoldToOpen);
            
            var pl = option.State.Transactions.Where(t => t.IsPL);

            Assert.Equal(1, pl.Count());
        }

        [Fact]
        public void PutOptionOperations()
        {
            var option = GetTestOption(_expiration);

            option.Sell(1, 10, DateTimeOffset.UtcNow.AddDays(-3), "some notes");
            
            Assert.Equal(-1, option.State.NumberOfContracts);
            Assert.Single(option.State.Transactions.Where(t => !t.IsPL));
            Assert.Empty(option.State.Transactions.Where(t => t.IsPL));
            Assert.Equal(3, option.State.DaysHeld);
            Assert.Equal(12, option.State.Days);
            Assert.Equal(10, option.State.DaysUntilExpiration);

            option.Buy(1, 1, DateTimeOffset.UtcNow, "some notes");

            Assert.Equal(0, option.State.NumberOfContracts);
            Assert.Equal(3, option.State.Transactions.Count);
            Assert.Equal(3, option.State.DaysHeld);
            Assert.Equal(12, option.State.Days);
            Assert.Equal(10, option.State.DaysUntilExpiration);

            option.Delete();

            Assert.Equal(0, option.State.NumberOfContracts);
            Assert.Empty(option.State.Transactions);
            Assert.Empty(option.State.Buys);
            Assert.Empty(option.State.Sells);
            Assert.Null(option.State.FirstFill);
            Assert.Null(option.State.SoldToOpen);
            Assert.Null(option.State.Closed);
            Assert.Empty(option.State.Notes);
            Assert.True(option.State.Deleted);
        }

        [Fact]
        public void Expire_CountsAsPLTransaction()
        {
            var option = GetTestOption(DateTimeOffset.UtcNow.AddDays(-1));

            option.Sell(1, 10, DateTimeOffset.UtcNow.AddDays(-3), "some notes");

            option.Expire(false);

            Assert.Equal(2, option.State.Transactions.Count);
            Assert.True(option.State.Transactions[1].IsPL);
            Assert.Equal(10, option.State.Transactions[1].Credit);
            Assert.Equal(1, option.State.Days);
            Assert.Equal(1, option.State.DaysHeld);
            Assert.Equal(0, option.State.DaysUntilExpiration);
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

        [Fact]
        public void MultipleBuySells()
        {
            var owned = new OwnedOption(
                new Ticker("SFIX"),
                23.5,
                OptionType.CALL,
                DateTimeOffset.Parse("2020-08-07"),
                Guid.NewGuid());

            owned.Buy(1, 100, DateTimeOffset.Parse("2020-07-16"), null);
            owned.Sell(1, 50, DateTimeOffset.Parse("2020-07-17"), null);

            owned.Buy(1, 100, DateTimeOffset.Parse("2020-07-16"), null);
            owned.Sell(1, 50, DateTimeOffset.Parse("2020-07-17"), null);

            var pl = owned.State.Transactions.Where(tx => tx.IsPL);

            var profit = pl.Sum(t => t.Profit);

            Assert.Equal(-100, profit);
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
