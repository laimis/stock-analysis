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
                cmd.Item1.Ticker,
                cmd.Item1.StrikePrice.Value,
                (OptionType)Enum.Parse(typeof(OptionType), cmd.Item1.OptionType),
                cmd.Item1.ExpirationDate.Value,
                cmd.Item2);

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
            var cmd = new OptionTransaction(
                strikePrice: 20,
                optionType: OptionType.PUT.ToString(),
                expirationDate: DateTimeOffset.UtcNow.AddDays(10),
                ticker: _ticker,
                numberOfContracts: 1,
                premium: 10,
                filled: DateTimeOffset.UtcNow,
                notes: null);
            return BuyOrSell.Command.NewSell(cmd, _user.Id);
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