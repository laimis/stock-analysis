using System;
using System.Threading.Tasks;
using core.Account;
using core.fs;
using core.fs.Options;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.CSV;
using core.fs.Adapters.Options;
using core.fs.Adapters.Storage;
using core.fs.Accounts;
using core.fs.Adapters.Logging;
using core.Shared;
using Microsoft.FSharp.Core;
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
            
            var query = new DetailsQuery(opt.State.Id, UserId.NewUserId(opt.State.UserId));

            var accountMock = new Mock<IAccountStorage>();
            accountMock.Setup(x => x.GetUser(UserId.NewUserId(opt.State.UserId)))
                .Returns(Task.FromResult(
                    new FSharpOption<User>(
                        User.Create("email", "f", "l")
                    )
                ));

            var brokerage = new Mock<IBrokerage>();
            brokerage.Setup(x => x.GetOptionChain(It.IsAny<UserState>(), It.IsAny<Ticker>()))
                .Returns(Task.FromResult(
                    FSharpResult<OptionChain,ServiceError>.NewOk(new OptionChain("TICKER", 0, 0, Array.Empty<OptionDetail>(), FSharpOption<decimal>.None))
                ));
            
            var handler = new OptionsHandler(accountMock.Object, brokerage.Object, storage, Mock.Of<ICSVWriter>(), Mock.Of<ILogger>());

            var result = await handler.Handle(query);

            Assert.True(result.IsOk);
        }
    }
}
