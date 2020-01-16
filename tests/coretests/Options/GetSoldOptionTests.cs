using System.Threading;
using System.Threading.Tasks;
using core.Options;
using Xunit;

namespace coretests.Options
{
    public class GetSoldOptionTests : IClassFixture<OptionsTestsFixture>
    {
        private OptionsTestsFixture _fixture;

        public GetSoldOptionTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Get_WorksAsync()
        {
            var query = new GetSoldOption.Query {
                Expiration = _fixture.CloseOptionCommand.Expiration.Value,
                Type = _fixture.CloseOptionCommand.OptionType,
                StrikePrice = _fixture.CloseOptionCommand.StrikePrice,
                Ticker = _fixture.CloseOptionCommand.Ticker,
                UserId = _fixture.CloseOptionCommand.UserIdentifier
            };

            var handler = new GetSoldOption.Handler(_fixture.CreateStorage());

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
        }
    }
}