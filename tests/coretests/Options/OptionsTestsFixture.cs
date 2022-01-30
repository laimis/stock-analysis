using System;
using core;
using core.Account;
using core.Options;
using core.Shared;
using Moq;

namespace coretests.Options
{
    public class OptionsTestsFixture
    {
        public static readonly Ticker Ticker = new Ticker("tlsa");
        public static User User;

        static OptionsTestsFixture()
        {
            var u = new User("email", "f", "l");
            u.Confirm();
            User = u;
        }

        public (IPortfolioStorage, OwnedOption) CreateStorageWithSoldOption()
        {
            var cmd = CreateSellCommand();

            var opt = new OwnedOption(
                cmd.Ticker,
                cmd.StrikePrice,
                (OptionType)Enum.Parse(typeof(OptionType), cmd.OptionType),
                cmd.ExpirationDate.Value,
                cmd.UserId);

            opt.Sell(1, 20, DateTimeOffset.UtcNow, "some note");

            var mock = new Mock<IPortfolioStorage>();

            mock.Setup(s => s.GetOwnedOptions(User.Id))
                .ReturnsAsync(new[] { opt });

            mock.Setup(s => s.GetOwnedOption(opt.Id, User.Id))
                .ReturnsAsync(opt);

            return (mock.Object, opt);
        }

        public static Sell.Command CreateSellCommand()
        {
            var cmd = new Sell.Command
            {
                Ticker = Ticker,
                NumberOfContracts = 1,
                Premium = 10,
                StrikePrice = 20,
                OptionType = OptionType.PUT.ToString(),
                ExpirationDate = DateTimeOffset.UtcNow.AddDays(10),
                Filled = DateTimeOffset.UtcNow
            };

            cmd.WithUserId(User.Id);

            return cmd;
        }

        internal IAccountStorage CreateAccountStorageWithUserAsync()
        {
            var mock = new Mock<IAccountStorage>();

            mock.Setup(s => s.GetUserByEmail(User.State.Email))
                .ReturnsAsync(User);

            mock.Setup(s => s.GetUser(User.State.Id))
                .ReturnsAsync(User);

            return mock.Object;
        }
    }
}