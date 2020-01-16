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
        public async Task NonExistingDoesNotFail()
        {
            var storage = new FakePortfolioStorage();

            var handler = new CloseOption.Handler(storage);

            await handler.Handle(_fixture.CloseOptionCommand, CancellationToken.None);

            Assert.Empty(storage.SavedOptions);
        }

        [Fact]
        public async Task ClosingSoldOption_Closes()
        {
            var storage = _fixture.CreateStorageWithSoldOption();

            var handler = new CloseOption.Handler(storage);

            await handler.Handle(_fixture.CloseOptionCommand, CancellationToken.None);

            var opt = storage.SavedOptions.Single();

            Assert.NotNull(opt.State.Closed);
        }
    }
}