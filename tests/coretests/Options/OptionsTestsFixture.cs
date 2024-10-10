using System;
using core.fs.Options;
using core.fs.Adapters.Storage;
using core.fs.Accounts;
using core.Options;
using core.Shared;
using Moq;
using OptionType = core.fs.Options.OptionType;

namespace coretests.Options
{
    public class OptionsTestsFixture
    {
        private static readonly Ticker _ticker = new("tlsa");
        private static readonly User _user;

        static OptionsTestsFixture()
        {
            var u = User.Create("email", "f", "l");
            u.Confirm();
            _user = u;
        }

        public (IPortfolioStorage, OwnedOption) CreateStorageWithSoldOption()
        {
            var cmd = CreateSellCommand() as BuyOrSellCommand.Sell;
            
            var opt = new OwnedOption(
                cmd.Item1.Ticker,
                cmd.Item1.StrikePrice.Value,
                core.Options.OptionType.CALL, // TODO: once this is migrated to use core.fs.Options, this should go back to being cmd.Item1.OptionType
                cmd.Item1.ExpirationDate.Value,
                cmd.Item2.Item);

            opt.Sell(1, 20, DateTimeOffset.UtcNow, "some note");

            var mock = new Mock<IPortfolioStorage>();

            mock.Setup(s => s.GetOwnedOptions(UserId.NewUserId(_user.Id)))
                .ReturnsAsync(new[] { opt });

            mock.Setup(s => s.GetOwnedOption(opt.Id, UserId.NewUserId(_user.Id)))
                .ReturnsAsync(opt);

            return (mock.Object, opt);
        }

        public static BuyOrSellCommand CreateSellCommand()
        {
            var cmd = new OptionTransaction(
                strikePrice: 20,
                optionType: OptionType.Call,
                expirationDate: DateTimeOffset.UtcNow.AddDays(10),
                ticker: _ticker,
                numberOfContracts: 1,
                premium: 10,
                filled: DateTimeOffset.UtcNow,
                notes: null);
            return BuyOrSellCommand.NewSell(cmd, UserId.NewUserId(_user.Id));
        }

        internal IAccountStorage CreateAccountStorageWithUserAsync()
        {
            var mock = new Mock<IAccountStorage>();

            mock.Setup(s => s.GetUserByEmail(_user.State.Email))
                .ReturnsAsync(_user);

            mock.Setup(s => s.GetUser(UserId.NewUserId(_user.State.Id)))
                .ReturnsAsync(_user);

            return mock.Object;
        }
    }
}
