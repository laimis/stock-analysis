using System.Threading.Tasks;
using core.fs.Options;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.CSV;
using core.fs.Adapters.Storage;
using core.fs.Accounts;
using core.Options;
using Moq;
using Xunit;
using Handler = core.fs.Options.Handler;

namespace coretests.Options
{
    public class SellOptionTests : IClassFixture<OptionsTestsFixture>
    {
        private readonly OptionsTestsFixture _fixture;

        public SellOptionTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Sell_OpensNewOneAsync()
        {
            var mock = new Mock<IPortfolioStorage>();

            mock.Setup(x => x.SaveOwnedOption(It.IsAny<OwnedOption>(), It.IsAny<UserId>()))
                .Callback((OwnedOption option, UserId _) =>
                {
                    Assert.Equal(-1, option.State.NumberOfContracts);
                });

            var account = _fixture.CreateAccountStorageWithUserAsync();

            var handler = new Handler(account, Mock.Of<IBrokerage>(), Mock.Of<IStockInfoProvider>(), mock.Object, Mock.Of<ICSVWriter>());

            await handler.Handle(
                OptionsTestsFixture.CreateSellCommand());
        }
    }
}
