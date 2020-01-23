using System;
using core.Options;
using coretests.Fakes;

namespace coretests.Options
{
    public class OptionsTestsFixture
    {
        public const string Ticker = "ticker";
        public const string UserId = "userid";

        private CloseOption.Command _closeOptionCommand;
        public CloseOption.Command CloseOptionCommand => _closeOptionCommand;

        private SellOption.Command _sellOptionCommand;
        public SellOption.Command SellOptionCommand => _sellOptionCommand;

        public OptionsTestsFixture()
        {
            _closeOptionCommand = CreateCloseCommand();
            _sellOptionCommand = CreateSellCommand();
        }

        public FakePortfolioStorage CreateStorageWithSoldOption()
        {
            return CreateStorage(_closeOptionCommand);
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

        private FakePortfolioStorage CreateStorage(CloseOption.Command cmd)
        {
            var storage = new FakePortfolioStorage();

            var opt = new SoldOption(cmd.Ticker, cmd.Type, cmd.Expiration.Value, cmd.StrikePrice, cmd.UserIdentifier);

            opt.Open(cmd.Amount, 200, DateTimeOffset.UtcNow);

            storage.Register(opt);

            return storage;
        }

        private static CloseOption.Command CreateCloseCommand()
        {
            var cmd = new CloseOption.Command
            {
                Amount = 1,
                CloseDate = DateTime.UtcNow,
                Expiration = DateTime.UtcNow.AddDays(1),
                ClosePrice = 0,
                OptionType = OptionType.CALL.ToString(),
                StrikePrice = 45,
                Ticker = Ticker
            };

            cmd.WithUser(UserId);

            return cmd;
        }

        private static SellOption.Command CreateSellCommand()
        {
            var cmd = new SellOption.Command
            {
                Amount = 1,
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                Filled = DateTime.UtcNow,
                Premium = 200,
                OptionType = OptionType.CALL.ToString(),
                StrikePrice = 45,
                Ticker = Ticker
            };

            cmd.WithUser(UserId);

            return cmd;
        }
    }
}