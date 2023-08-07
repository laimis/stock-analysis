using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Options;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using Moq;
using Xunit;

namespace coretests.Options
{
    public class DetailsTests : IClassFixture<OptionsTestsFixture>
    {
        private readonly OptionsTestsFixture _fixture;

        public DetailsTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Get_WorksAsync()
        {
            var (storage, opt) = _fixture.CreateStorageWithSoldOption();
            
            var query = new Details.Query(opt.State.Id, opt.State.UserId);

            var accountMock = new Mock<IAccountStorage>();
            accountMock.Setup(x => x.GetUser(opt.State.UserId))
                .Returns(Task.FromResult(
                    new User("email", "f", "l")
                ));

            var handler = new Details.Handler(accountMock.Object, storage);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
        }
    }
}