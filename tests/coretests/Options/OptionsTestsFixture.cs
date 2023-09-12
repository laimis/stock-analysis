using System;
using core;
using core.Account;
using core.fs.Options;
using core.Options;
using core.Shared;
using core.Shared.Adapters.Storage;
using core.Stocks;
using Moq;

namespace coretests.Options
{
    public class OptionsTestsFixture
    {
        private static readonly Ticker _ticker = new("tlsa");
        private static readonly User _user;

        static OptionsTestsFixture()
        {
            var u = new User("email", "f", "l");
            u.Confirm();
            _user = u;
        }

        public (IPortfolioStorage, OwnedOption) CreateStorageWithSoldOption()
        {
            var cmd = CreateSellCommand() as BuyOrSell.Command.Sell;
            

            var opt = new OwnedOption(
                cmd.Item.Ticker,
                cmd.Item.StrikePrice.Value,
                (OptionType)Enum.Parse(typeof(OptionType), cmd.Item.OptionType),
                cmd.Item.ExpirationDate.Value,
                cmd.Item.UserId);

            opt.Sell(1, 20, DateTimeOffset.UtcNow, "some note");

            var mock = new Mock<IPortfolioStorage>();

            mock.Setup(s => s.GetOwnedOptions(_user.Id))
                .ReturnsAsync(new[] { opt });

            mock.Setup(s => s.GetOwnedOption(opt.Id, _user.Id))
                .ReturnsAsync(opt);

            return (mock.Object, opt);
        }

        public static BuyOrSell.Command CreateSellCommand()
        {
            var cmd = new OptionTransaction()
            {
                Ticker = _ticker,
                NumberOfContracts = 1,
                Premium = 10,
                StrikePrice = 20,
                OptionType = OptionType.PUT.ToString(),
                ExpirationDate = DateTimeOffset.UtcNow.AddDays(10),
                Filled = DateTimeOffset.UtcNow,
                UserId = _user.Id
            };
            return BuyOrSell.Command.NewSell(cmd);
        }

        internal IAccountStorage CreateAccountStorageWithUserAsync()
        {
            var mock = new Mock<IAccountStorage>();

            mock.Setup(s => s.GetUserByEmail(_user.State.Email))
                .ReturnsAsync(_user);

            mock.Setup(s => s.GetUser(_user.State.Id))
                .ReturnsAsync(_user);

            return mock.Object;
        }
    }
}