using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.fs.Options;
using core.Options;
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

            mock.Setup(x => x.Save(It.IsAny<OwnedOption>(), It.IsAny<Guid>()))
                .Callback((OwnedOption option, Guid userId) =>
                {
                    Assert.Equal(-1, option.State.NumberOfContracts);
                });

            var account = _fixture.CreateAccountStorageWithUserAsync();

            var handler = new BuyOrSell.Handler(account, mock.Object);

            await handler.Handle(
                OptionsTestsFixture.CreateSellCommand());
        }
    }
}