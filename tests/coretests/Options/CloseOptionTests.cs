using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Options;
using coretests.Fakes;
using Xunit;

namespace coretests.Options
{
    public class CloseOptionTests : IClassFixture<OptionsTestsFixture>
    {
        private OptionsTestsFixture _fixture;

        public CloseOptionTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task SellingOption_Decreases()
        {
            var storage = _fixture.CreateStorageWithNoOptions();

            var handler = new Sell.Handler(storage);

            await handler.Handle(
                OptionsTestsFixture.CreateSellCommand(),
                CancellationToken.None);

            await handler.Handle(
                OptionsTestsFixture.CreateSellCommand(),
                CancellationToken.None);

            var opt = storage.SavedOptions.Single();

            Assert.Equal(-2, opt.State.NumberOfContracts);
        }
    }
}