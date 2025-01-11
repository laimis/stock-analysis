using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using core.fs;
using core.fs.Options;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.CSV;
using core.fs.Accounts;
using core.fs.Adapters.Logging;
using core.fs.Adapters.Storage;
using core.Shared;
using Microsoft.FSharp.Core;
using Moq;
using Xunit;

namespace coretests.Options
{
    public class ListTests : IClassFixture<OptionsTestsFixture>
    {
        private readonly OptionsTestsFixture _fixture;

        public ListTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task List_WorksAsync()
        {
            var (storage, _) = _fixture.CreateStorageWithSoldOption();
            
            var query = new DashboardQuery(UserId.NewUserId(Guid.NewGuid()));

            var accounts = new Mock<IAccountStorage>();
            accounts.Setup(x => x.GetUser(It.IsAny<UserId>()))
                .Returns(Task.FromResult(
                        new FSharpOption<User>(
                            User.Create("e", "f", "l"))
                    )
                );

            var brokerage = new Mock<IBrokerage>();
            brokerage.Setup(x => x.GetQuotes(It.IsAny<UserState>(), It.IsAny<List<Ticker>>()))
                .Returns(Task.FromResult(
                    FSharpResult<Dictionary<Ticker, StockQuote>, ServiceError>.NewOk(
                        new Dictionary<Ticker, StockQuote>()
                    )
                ));

            brokerage.Setup(x => x.GetAccount(It.IsAny<UserState>()))
                .Returns(Task.FromResult(
                    FSharpResult<BrokerageAccount,ServiceError>.NewOk(
                        new BrokerageAccount {
                            OptionPositions = Array.Empty<BrokerageOptionPosition>(),
                            StockPositions = Array.Empty<BrokerageStockPosition>(),
                        }
                    )
                ));
            
            var handler = new OptionsHandler(accounts.Object, brokerage.Object, storage, Mock.Of<ICSVWriter>(), Mock.Of<ILogger>());

            var result = await handler.Handle(query);

            Assert.Equal(0, result.ResultValue.BuyStats.Count);
        }
    }
}
