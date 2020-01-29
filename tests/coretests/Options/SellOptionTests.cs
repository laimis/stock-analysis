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
            var storage = _fixture.CreateStorageWithNoSoldOptions();

            var handler = new Open.Handler(storage);

            await handler.Handle(_fixture.OpenOptionCommand, CancellationToken.None);

            Assert.Single(storage.SavedOptions);
            Assert.True(storage.SavedOptions.Single().State.IsOpen);
        }
    }
}