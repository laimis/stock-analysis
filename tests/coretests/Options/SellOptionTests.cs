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

            var handler = new SellOption.Handler(storage);

            await handler.Handle(_fixture.SellOptionCommand, CancellationToken.None);

            Assert.Single(storage.SavedOptions);
        }

        [Fact]
        public async Task Sell_AlreadySold_IncreasesAmount()
        {
            var storage = _fixture.CreateStorageWithNoSoldOptions();

            var handler = new SellOption.Handler(storage);

            await handler.Handle(_fixture.SellOptionCommand, CancellationToken.None);

            Assert.Single(storage.SavedOptions);

            var opt = storage.SavedOptions.Single();
            
            storage = _fixture.CreateStorageWithSoldOption(opt);

            handler = new SellOption.Handler(storage);

            await handler.Handle(_fixture.SellOptionCommand, CancellationToken.None);

            opt = storage.SavedOptions.Single();

            Assert.Equal(2, opt.State.Amount);
            Assert.Null(opt.State.Closed);
        }
    }
}