using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Options;
using Xunit;

namespace coretests.Options
{
    public class SellOptionTests : IClassFixture<OptionsTestsFixture>
    {
        private OptionsTestsFixture _fixture;

        public SellOptionTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Sell_OpensNewOneAsync()
        {
            var storage = _fixture.CreateStorageWithNoOptions();

            var handler = new Sell.Handler(storage);

            await handler.Handle(OptionsTestsFixture.CreateSellCommand(), CancellationToken.None);

            Assert.Single(storage.SavedOptions);
            Assert.Equal(-1, storage.SavedOptions.Single().State.NumberOfContracts);
        }
    }
}