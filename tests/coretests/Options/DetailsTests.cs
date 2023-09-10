using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Options;
using core.fs.Options;
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

            var brokerage = new Mock<IBrokerage>();
            brokerage.Setup(x => x.GetOptions(It.IsAny<UserState>(), It.IsAny<string>(), null, null, null))
                .Returns(Task.FromResult(
                    new ServiceResponse<OptionChain>(new OptionChain("TICKER", 0, 0, Array.Empty<OptionDetail>()))
                ));
            
            var handler = new Details.Handler(accountMock.Object, brokerage.Object, storage);

            var result = await handler.Handle(query);

            Assert.NotNull(result);
        }
    }
}