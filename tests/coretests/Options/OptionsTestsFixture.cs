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

        private Open.Command _openOptionCommand;
        public Open.Command OpenOptionCommand => _openOptionCommand;

        public OptionsTestsFixture()
        {
            _closeOptionCommand = CreateCloseCommand();
            _openOptionCommand = CreateOpenCommand();
        }

        public FakePortfolioStorage CreateStorageWithSoldOption()
        {
            return CreateStorage(_openOptionCommand);
        }

        public FakePortfolioStorage CreateStorageWithNoSoldOptions()
        {
            return new FakePortfolioStorage();
        }

        public FakePortfolioStorage CreateStorageWithSoldOption(OwnedOption opt)
        {
            var storage = new FakePortfolioStorage();
            storage.Register(opt);
            return storage;
        }

        private FakePortfolioStorage CreateStorage(Open.Command open)
        {
            var storage = new FakePortfolioStorage();

            var opt = new OwnedOption(
                open.Ticker,
                (PositionType)Enum.Parse(typeof(PositionType), open.PositionType),
                (OptionType)Enum.Parse(typeof(OptionType), open.OptionType),
                open.ExpirationDate.Value,
                open.StrikePrice,
                open.UserId,
                open.Amount,
                open.Premium,
                DateTimeOffset.UtcNow);

            storage.Register(opt);

            return storage;
        }

        private static Close.Command CreateCloseCommand()
        {
            var cmd = new Close.Command
            {
                Id = Guid.NewGuid(),
                NumberOfContracts = 1,
                CloseDate = DateTime.UtcNow,
                ClosePrice = 0,
            };

            cmd.WithUserId(UserId);

            return cmd;
        }

        private static Open.Command CreateOpenCommand()
        {
            var cmd = new Open.Command
            {
                Amount = 1,
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                Filled = DateTime.UtcNow,
                Premium = 200,
                PositionType = PositionType.Sell.ToString(),
                OptionType = OptionType.CALL.ToString(),
                StrikePrice = 45,
                Ticker = Ticker
            };

            cmd.WithUserId(UserId);

            return cmd;
        }
    }
}