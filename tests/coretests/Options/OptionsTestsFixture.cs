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

        public OptionsTestsFixture()
        {
            _closeOptionCommand = CreateCommand();
        }

        public FakePortfolioStorage CreateStorage()
        {
            return CreateStorage(_closeOptionCommand);
        }

        public FakePortfolioStorage CreateStorage(CloseOption.Command cmd)
        {
            var storage = new FakePortfolioStorage();

            var opt = new SoldOption(cmd.Ticker, cmd.Type, cmd.Expiration.Value, cmd.StrikePrice, cmd.UserIdentifier);

            opt.Open(cmd.Amount, 200, DateTimeOffset.UtcNow);

            storage.Register(opt);

            return storage;
        }

        private static CloseOption.Command CreateCommand()
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
    }
}