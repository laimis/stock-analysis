using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using core.fs.Options;
using core.fs.Shared.Adapters.Brokerage;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
using core.Shared;
using core.Shared.Adapters.CSV;
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
                    new ServiceResponse<Dictionary<string, StockQuote>>(
                        new Dictionary<string, StockQuote>()
                    )
                ));

            brokerage.Setup(x => x.GetAccount(It.IsAny<UserState>()))
                .Returns(Task.FromResult(
                    new ServiceResponse<TradingAccount>(
                        new TradingAccount {
                            OptionPositions = Array.Empty<OptionPosition>(),
                            StockPositions = Array.Empty<StockPosition>(),
                        }
                    )
                ));
            
            var handler = new Handler(accounts.Object, brokerage.Object, storage, Mock.Of<ICSVWriter>());

            var result = await handler.Handle(query);

            Assert.Equal(0, result.Success.BuyStats.Count);
        }
    }
}