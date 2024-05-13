﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using core.fs;
using core.fs.Options;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.CSV;
using core.fs.Accounts;
using core.fs.Adapters.Storage;
using core.Shared;
using Microsoft.FSharp.Core;
using Moq;
using Xunit;
using Handler = core.fs.Options.Handler;

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

            var stockInfoProvider = new Mock<IStockInfoProvider>();
            stockInfoProvider.Setup(x => x.GetQuotes(It.IsAny<UserState>(), It.IsAny<List<Ticker>>()))
                .Returns(Task.FromResult(
                    FSharpResult<IDictionary<Ticker, StockQuote>, ServiceError>.NewOk(
                        new Dictionary<Ticker, StockQuote>()
                    )
                ));

            var brokerage = new Mock<IBrokerage>();
            brokerage.Setup(x => x.GetAccount(It.IsAny<UserState>()))
                .Returns(Task.FromResult(
                    FSharpResult<BrokerageAccount,ServiceError>.NewOk(
                        new BrokerageAccount {
                            OptionPositions = [],
                            StockPositions = [],
                        }
                    )
                ));
            
            var handler = new Handler(accounts.Object, brokerage.Object, stockInfoProvider.Object, storage, Mock.Of<ICSVWriter>());

            var result = await handler.Handle(query);

            Assert.Equal(0, result.ResultValue.BuyStats.Count);
        }
    }
}
