using System;
using System.Threading.Tasks;
using core.fs.Options;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
using core.Options;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.CSV;
using Moq;
using Xunit;

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

            var handler = new Handler(account, Mock.Of<IBrokerage>(), mock.Object, Mock.Of<ICSVWriter>());

            await handler.Handle(
                OptionsTestsFixture.CreateSellCommand());
        }
    }
}