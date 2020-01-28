using System;
using core.Options;
using coretests.Fakes;

namespace coretests.Options
{
    public class OptionsTestsFixture
    {
        public const string Ticker = "ticker";
        public const string UserId = "userid";

        private Close.Command _closeOptionCommand;
        public Close.Command CloseOptionCommand => _closeOptionCommand;

        private Sell.Command _sellOptionCommand;
        public Sell.Command SellOptionCommand => _sellOptionCommand;

        public OptionsTestsFixture()
        {
            _closeOptionCommand = CreateCloseCommand();
            _sellOptionCommand = CreateSellCommand();
        }

        public FakePortfolioStorage CreateStorageWithSoldOption()
        {
            return CreateStorage(_sellOptionCommand);
        }

        public FakePortfolioStorage CreateStorageWithNoSoldOptions()
        {
            return new FakePortfolioStorage();
        }

        public FakePortfolioStorage CreateStorageWithSoldOption(SoldOption opt)
        {
            var storage = new FakePortfolioStorage();
            storage.Register(opt);
            return storage;
        }

        private FakePortfolioStorage CreateStorage(Sell.Command sell)
        {
            var storage = new FakePortfolioStorage();

            var opt = new SoldOption(
                sell.Ticker,
                (OptionType)Enum.Parse(typeof(OptionType), sell.OptionType),
                sell.ExpirationDate.Value,
                sell.StrikePrice,
                sell.UserId,
                sell.Amount,
                sell.Premium,
                DateTimeOffset.UtcNow);

            storage.Register(opt);

            return storage;
        }

        private static Close.Command CreateCloseCommand()
        {
            var cmd = new Close.Command
            {
                Amount = 1,
                CloseDate = DateTime.UtcNow,
                ClosePrice = 0,
            };

            cmd.WithUserId(UserId);

            return cmd;
        }

        private static Sell.Command CreateSellCommand()
        {
            var cmd = new Sell.Command
            {
                Amount = 1,
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                Filled = DateTime.UtcNow,
                Premium = 200,
                OptionType = OptionType.CALL.ToString(),
                StrikePrice = 45,
                Ticker = Ticker
            };

            cmd.WithUserId(UserId);

            return cmd;
        }
    }
}