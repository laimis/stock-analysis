using System;
using System.Threading.Tasks;
using core.Account;
using core.fs.Options;
using core.fs.Shared.Adapters.Brokerage;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
using core.Shared;
using core.Shared.Adapters.CSV;
using core.Shared.Adapters.Options;
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
            brokerage.Setup(x => x.GetOptions(It.IsAny<UserState>(), It.IsAny<Ticker>(), null, null, null))
                .Returns(Task.FromResult(
                    new ServiceResponse<OptionChain>(new OptionChain("TICKER", 0, 0, Array.Empty<OptionDetail>()))
                ));
            
            var handler = new Handler(accountMock.Object, brokerage.Object, storage, Mock.Of<ICSVWriter>());

            var result = await handler.Handle(query);

            Assert.NotNull(result);
        }
    }
}