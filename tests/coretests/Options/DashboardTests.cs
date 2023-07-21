using System;
using System.Collections.Generic;
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
    public class ListTests : IClassFixture<OptionsTestsFixture>
    {
        private OptionsTestsFixture _fixture;

        public ListTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task List_WorksAsync()
        {
            var (storage, _) = _fixture.CreateStorageWithSoldOption();
            
            var query = new Dashboard.Query(Guid.NewGuid());

            var accounts = new Mock<IAccountStorage>();
            accounts.Setup(x => x.GetUser(It.IsAny<Guid>()))
                .Returns(Task.FromResult(
                    new User("e", "f", "l"))
                );

            var brokerage = new Mock<IBrokerage>();
            brokerage.Setup(x => x.GetQuotes(It.IsAny<UserState>(), It.IsAny<List<string>>()))
                .Returns(Task.FromResult(
                    new ServiceResponse<Dictionary<string, StockQuote>>(
                        new Dictionary<string, StockQuote>()
                    )
                ));
            
            var handler = new Dashboard.Handler(accounts.Object, brokerage.Object, storage);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Equal(0, result.BuyStats.Count);
        }
    }
}