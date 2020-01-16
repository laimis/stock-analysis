using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace coretests.Options
{
    public class ExportTests : IClassFixture<OptionsTestsFixture>
    {
        public ExportTests(OptionsTestsFixture fixture)
        {
            this.Fixture = fixture;
        }

        public OptionsTestsFixture Fixture { get; }

        [Fact]
        public async Task ClosingSoldOption_Closes()
        {
            var handler = new core.Options.Export.Handler(Fixture.CreateStorage());

            var query = new core.Options.Export.Query(OptionsTestsFixture.UserId);

            var response = await handler.Handle(query, CancellationToken.None);

            Assert.Contains("options", response.Filename);
            Assert.NotNull(response.Content);
            Assert.Contains(OptionsTestsFixture.Ticker, response.Content);
        }
    }
}