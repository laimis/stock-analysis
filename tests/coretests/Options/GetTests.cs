using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Options;
using Xunit;

namespace coretests.Options
{
    public class GetTests : IClassFixture<OptionsTestsFixture>
    {
        private OptionsTestsFixture _fixture;

        public GetTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Get_WorksAsync()
        {
            var storage = _fixture.CreateStorageWithSoldOption();
            var opt = storage.SavedOptions.First();
            var query = new Get.Query {
                Id = opt.State.Id
            };

            query.WithUserId(opt.State.UserId);

            var handler = new Get.Handler(storage);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
        }
    }
}