using System;
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
            var query = new Get.Query {
                Id = Guid.NewGuid()
            };

            query.WithUserId(_fixture.CloseOptionCommand.UserId);

            var handler = new Get.Handler(_fixture.CreateStorageWithSoldOption());

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
        }
    }
}