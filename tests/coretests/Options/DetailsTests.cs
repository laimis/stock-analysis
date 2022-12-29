using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Options;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using Moq;
using Xunit;

namespace coretests.Options
{
    public class DetailsTests : IClassFixture<OptionsTestsFixture>
    {
        private OptionsTestsFixture _fixture;

        public DetailsTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Get_WorksAsync()
        {
            var (storage, opt) = _fixture.CreateStorageWithSoldOption();
            
            var query = new Details.Query(opt.State.Id, opt.State.UserId);

            var brokerageMock = new Mock<IBrokerage>();
            brokerageMock.Setup(x => x.GetQuote(It.IsAny<UserState>(), opt.State.Ticker))
                .Returns(Task.FromResult(
                    new ServiceResponse<StockQuote>(
                        new StockQuote { lastPrice = 100 }
                    )
                ));

            var accountMock = new Mock<IAccountStorage>();
            accountMock.Setup(x => x.GetUser(opt.State.UserId))
                .Returns(Task.FromResult(
                    new User("email", "f", "l")
                ));

            var handler = new Details.Handler(accountMock.Object, brokerageMock.Object, storage);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
        }
    }
}