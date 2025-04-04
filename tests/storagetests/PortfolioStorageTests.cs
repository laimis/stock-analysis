using System;
using System.Linq;
using System.Threading.Tasks;
using core.Cryptos;
using core.fs.Adapters.Storage;
using core.fs.Accounts;
using core.fs.Options;
using core.fs.Stocks;
using core.Routines;
using core.Shared;
using core.Stocks;
using Microsoft.FSharp.Core;
using testutils;
using Xunit;
using Xunit.Abstractions;

namespace storagetests
{
    public abstract class PortfolioStorageTests
    {
        private static readonly UserId _userId = UserId.NewUserId(Guid.NewGuid());
        private readonly ITestOutputHelper _output;

        public PortfolioStorageTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task NonExistingStockReturnsNullAsync()
        {
            var storage = CreateStorage();

            Assert.Null(await storage.GetStockPosition(StockPositionId.NewStockPositionId(Guid.NewGuid()), _userId));
        }

        protected abstract IPortfolioStorage CreateStorage();

        [Fact]
        public async Task NonExistingOptionReturnsNullAsync()
        {
            var storage = CreateStorage();

            Assert.Null(await storage.GetOptionPosition(OptionPositionId.NewOptionPositionId(Guid.NewGuid()), _userId));
        }
        
        [Fact]
        public async Task StockPosition_Works()
        {
            void AssertPositions(FSharpOption<StockPositionState> expected, FSharpOption<StockPositionState> actual)
            {
                Assert.Equal(expected.Value.NumberOfShares, actual.Value.NumberOfShares);
                Assert.Equal(expected.Value.Ticker, actual.Value.Ticker);
                Assert.Equal(expected.Value.PositionId, actual.Value.PositionId);
                Assert.Equal(expected.Value.Opened, actual.Value.Opened);
            }
            
            var position = StockPosition.openLong(GenerateTestTicker(), DateTimeOffset.UtcNow);
            
            position = StockPosition.buy(10m, 2.1m, DateTimeOffset.UtcNow, position);

            var storage = CreateStorage();

            await storage.SaveStockPosition(_userId, FSharpOption<StockPositionState>.None, position);

            var loadedList = await storage.GetStockPositions(_userId);

            Assert.NotEmpty(loadedList);

            var loaded = await storage.GetStockPosition(position.PositionId, _userId);

            AssertPositions(position, loaded);

            var afterPurchase = StockPosition.buy(5m, 5m, DateTimeOffset.UtcNow, loaded.Value);
            
            await storage.SaveStockPosition(_userId, loaded, afterPurchase);
            
            var afterDividend = StockPosition.processDividend("activityid", DateTimeOffset.UtcNow, "some divident", 1m, afterPurchase);

            await storage.SaveStockPosition(_userId, afterPurchase, afterDividend);
            
            var afterFee = StockPosition.processFee("activityid", DateTimeOffset.UtcNow, "some fee", 1m, afterDividend);
            
            await storage.SaveStockPosition(_userId, afterDividend, afterFee);
            
            var final = afterFee;
            
            var reloaded = await storage.GetStockPosition(afterPurchase.PositionId, _userId);

            AssertPositions(final, reloaded);

            await storage.Delete(_userId);
            
            var afterDelete = await storage.GetStockPositions(_userId);
            
            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task StockPosition_ExplicitDelete_Works()
        {
            var position = StockPosition.openLong(GenerateTestTicker(), DateTimeOffset.UtcNow);
            position = StockPosition.buy(10m, 2.1m, DateTimeOffset.UtcNow, position);
            
            var storage = CreateStorage();
            
            await storage.SaveStockPosition(_userId, FSharpOption<StockPositionState>.None, position);
            
            var loaded = await storage.GetStockPosition(position.PositionId, _userId);

            Assert.NotNull(loaded);

            var deleted = StockPosition.delete(loaded.Value);
            
            await storage.DeleteStockPosition(_userId, loaded, deleted);
            
            var afterDelete = await storage.GetStockPosition(loaded.Value.PositionId, _userId);
            
            Assert.Null(afterDelete);
        }

        private static Ticker GenerateTestTicker()
        {
            return new Ticker($"test-{Guid.NewGuid().ToString("N")[..4]}");
        }

        [Fact]
        public async Task OwnedOption_WorksAsync()
        {
            var expiration = DateTimeOffset.UtcNow.AddDays(30).Date;

            var option = OptionPosition.open(
                GenerateTestTicker(),
                DateTimeOffset.UtcNow
            );

            var storage = CreateStorage();

            await storage.SaveOptionPosition(_userId, FSharpOption<OptionPositionState>.None, option);

            var loaded = await storage.GetOptionPosition(option.PositionId, _userId);

            Assert.NotNull(loaded.Value);

            Assert.Equal(option.UnderlyingTicker, loaded.Value.UnderlyingTicker);

            var list = await storage.GetOptionPositions(_userId);

            var fromList = list.Single(o => o.UnderlyingTicker.Equals(option.UnderlyingTicker));

            await storage.Delete(_userId);

            var afterDelete = await storage.GetOptionPositions(_userId);

            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task OwnedCryptoWorksAsync()
        {
            var crypto = new OwnedCrypto(new Token("BTC"), IdentifierHelper.getUserId(_userId));

            crypto.Purchase(10, 2.1m, DateTimeOffset.UtcNow);

            var storage = CreateStorage();

            await storage.SaveCrypto(crypto, _userId);

            var loadedList = await storage.GetCryptos(_userId);

            Assert.NotEmpty(loadedList);

            var loaded = await storage.GetCrypto(crypto.State.Token, _userId);

            Assert.Equal(loaded.State.Quantity, crypto.State.Quantity);

            loaded.Purchase(5, 5, DateTime.UtcNow);

            await storage.SaveCrypto(loaded, _userId);

            loaded = await storage.GetCrypto(loaded.State.Token, UserId.NewUserId(loaded.State.UserId));

            Assert.NotEqual(loaded.State.Quantity, crypto.State.Quantity);

            await storage.Delete(_userId);

            var afterDelete = await storage.GetCryptos(_userId);

            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task StockPosition_Grading_Works()
        {
            var stock = StockPosition.openLong(GenerateTestTicker(), DateTimeOffset.UtcNow);

            var afterPurchase = StockPosition.buy(10m, 2.1m, DateTimeOffset.UtcNow, stock);
                
            var storage = CreateStorage();

            await storage.SaveStockPosition(_userId, FSharpOption<StockPositionState>.None, afterPurchase);

            var loaded = await storage.GetStockPosition(afterPurchase.PositionId, _userId);

            var afterSell = StockPosition.sell(10m, 2.2m, DateTimeOffset.UtcNow, loaded.Value);
            
            var afterGrading = StockPosition.assignGrade(TestDataGenerator.A, new FSharpOption<string>("test"), DateTimeOffset.UtcNow, afterSell);
            
            await storage.SaveStockPosition(_userId, loaded, afterGrading);

            loaded = await storage.GetStockPosition(afterGrading.PositionId, _userId);

            Assert.Equal(TestDataGenerator.A, loaded.Value.Grade);    
            
            await storage.Delete(_userId);
        }

        [Fact]
        public async Task Routines_Works()
        {
            var storage = CreateStorage();

            var existing = await storage.GetRoutines(_userId);

            Assert.Empty(existing);

            var routine = new Routine("description", "name", _userId.Item);

            routine.AddStep("step", "url");

            await storage.SaveRoutine(routine, _userId);

            existing = await storage.GetRoutines(_userId);

            Assert.NotEmpty(existing);

            var loaded = await storage.GetRoutine(routine.State.Id, _userId);

            Assert.Equal(routine.State.Name, loaded.State.Name);

            var step = loaded.State.Steps.SingleOrDefault(s => s.label == "step");

            Assert.NotNull(step.label);

            await storage.DeleteRoutine(loaded, _userId);

            existing = await storage.GetRoutines(_userId);

            Assert.Empty(existing);
        }

        [Fact]
        public async Task StockList_Works()
        {
            var storage = CreateStorage();

            var existing = await storage.GetStockLists(_userId);

            Assert.Empty(existing);

            var list = new StockList("description", "name", _userId.Item);

            list.AddStock(TestDataGenerator.AMD, "yeah yeah");

            await storage.SaveStockList(list, _userId);

            existing = await storage.GetStockLists(_userId);

            Assert.NotEmpty(existing);

            var loaded = await storage.GetStockList(list.State.Id, _userId);

            Assert.Equal(list.State.Name, loaded.State.Name);

            var ticker = loaded.State.Tickers.SingleOrDefault(t => t.Ticker.Equals(TestDataGenerator.AMD));

            Assert.NotNull(ticker);

            await storage.DeleteStockList(loaded, _userId);

            existing = await storage.GetStockLists(_userId);

            Assert.Empty(existing);

            await storage.Delete(_userId);
        }

        [Fact]
        public async Task PendingStockPosition_Works()
        {
            var storage = CreateStorage();

            var existing = await storage.GetPendingStockPositions(_userId);

            Assert.Empty(existing);

            var position = new PendingStockPosition(
                notes: "notes",
                numberOfShares: 10,
                price: 2.1m,
                stopPrice: 2m,
                sizeStopPrice: 1.9m,
                strategy: "strategy",
                ticker: TestDataGenerator.AMD,
                userId: _userId.Item
            );

            await storage.SavePendingPosition(position, _userId);

            existing = await storage.GetPendingStockPositions(_userId);

            Assert.Single(existing);

            var loaded = existing.Single(p => p.State.Ticker.Equals(TestDataGenerator.AMD));

            Assert.Equal(position.State.Id, loaded.State.Id);

            loaded.Purchase(price: 2.2m);

            await storage.SavePendingPosition(loaded, _userId);

            existing = await storage.GetPendingStockPositions(_userId);

            Assert.NotEmpty(existing);

            loaded = existing.Single(p => p.State.Ticker.Equals(TestDataGenerator.AMD));

            Assert.True(loaded.State.IsClosed);
            Assert.NotNull(loaded.State.Closed);
            Assert.True(loaded.State.Purchased);

            await storage.Delete(_userId);
        }
        
    }
}
