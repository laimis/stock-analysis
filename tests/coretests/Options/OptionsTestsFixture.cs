using System;
using core.Options;
using coretests.Fakes;

namespace coretests.Options
{
    public class OptionsTestsFixture
    {
        public const string Ticker = "ticker";
        public static Guid UserId = Guid.NewGuid();

        public FakePortfolioStorage CreateStorageWithSoldOption()
        {
            var storage = new FakePortfolioStorage();

            var cmd = CreateSellCommand();

            var opt = new OwnedOption(
                cmd.Ticker,
                cmd.StrikePrice,
                (OptionType)Enum.Parse(typeof(OptionType), cmd.OptionType),
                cmd.ExpirationDate.Value,
                cmd.UserId);

            opt.Sell(1, 20, DateTimeOffset.UtcNow);

            storage.Register(opt);

            return storage;
        }

        public FakePortfolioStorage CreateStorageWithNoOptions()
        {
            return new FakePortfolioStorage();
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

            cmd.WithUserId(UserId);

            return cmd;
        }
    }
}