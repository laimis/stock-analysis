using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace coretests.Options
{
    public class ExportTests : IClassFixture<OptionsTestsFixture>
    {
        public ExportTests(OptionsTestsFixture fixture)
        {
            Fixture = fixture;
        }

        public OptionsTestsFixture Fixture { get; }

        [Fact]
        public async Task ExportContainsExpectedData()
        {
            var (storage, _) = Fixture.CreateStorageWithSoldOption();
            
            var handler = new core.Options.Export.Handler(storage);

            var query = new core.Options.Export.Query(OptionsTestsFixture.User.Id);

            var response = await handler.Handle(query, CancellationToken.None);

            Assert.Contains("options", response.Filename);
            Assert.NotNull(response.Content);
            Assert.Contains(OptionsTestsFixture.Ticker, response.Content);
        }
    }
}