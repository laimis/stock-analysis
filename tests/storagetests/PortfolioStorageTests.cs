using System;
using System.Linq;
using System.Threading.Tasks;
using core.Cryptos;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
using core.Notes;
using core.Options;
using core.Routines;
using core.Shared;
using core.Stocks;
using coretests.testdata;
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

            Assert.Null(await storage.GetStock(new Ticker("nonexisting"), _userId));
        }

        protected abstract IPortfolioStorage CreateStorage();

        [Fact]
        public async Task NonExistingOptionReturnsNullAsync()
        {
            var storage = CreateStorage();

            Assert.Null(await storage.GetOwnedOption(Guid.NewGuid(), _userId));
        }

        [Fact]
        public async Task OwnedStockStorageAsync()
        {
            var stock = new OwnedStock(GenerateTestTicker(), _userId.Item);

            stock.Purchase(10, 2.1m, DateTime.UtcNow);

            var storage = CreateStorage();

            await storage.Save(stock, _userId);

            var loadedList = await storage.GetStocks(_userId);

            Assert.NotEmpty(loadedList);

            var loaded = await storage.GetStock(stock.State.Ticker, _userId);

            Assert.Equal(loaded.State.OpenPosition.NumberOfShares, stock.State.OpenPosition.NumberOfShares);

            loaded.Purchase(5, 5, DateTime.UtcNow);

            await storage.Save(loaded, _userId);

            loaded = await storage.GetStock(loaded.State.Ticker, UserId.NewUserId(loaded.State.UserId));

            Assert.NotEqual(loaded.State.OpenPosition.NumberOfShares, stock.State.OpenPosition.NumberOfShares);

            await storage.Delete(_userId);

            var afterDelete = await storage.GetStocks(_userId);

            Assert.Empty(afterDelete);
        }

        private static Ticker GenerateTestTicker()
        {
            return new Ticker($"test-{Guid.NewGuid().ToString("N")[..4]}");
        }

        [Fact]
        public async Task OwnedOption_WorksAsync()
        {
            var expiration = DateTimeOffset.UtcNow.AddDays(30).Date;

            var option = new OwnedOption(
                GenerateTestTicker(),
                2.5m,
                OptionType.CALL,
                expiration,
                _userId.Item
            );

            var storage = CreateStorage();

            await storage.SaveOwnedOption(option, _userId);

            var loaded = await storage.GetOwnedOption(
                option.State.Id,
                _userId);

            Assert.NotNull(loaded);

            Assert.Equal(option.State.StrikePrice, loaded.State.StrikePrice);

            var list = await storage.GetOwnedOptions(_userId);

            var fromList = list.Single(o => o.State.Ticker.Equals(option.State.Ticker));

            Assert.Equal(option.State.StrikePrice, fromList.State.StrikePrice);

            await storage.Delete(_userId);

            var afterDelete = await storage.GetOwnedOptions(_userId);

            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task NoteStorageWorksAsync()
        {
            var note = new Note(_userId.Item, "note", TestDataGenerator.TSLA, DateTimeOffset.UtcNow);

            var storage = CreateStorage();

            await storage.SaveNote(note, _userId);

            var notes = await storage.GetNotes(_userId);

            Assert.NotEmpty(notes);

            note = notes.Single(n => n.State.Id == note.State.Id);

            note.Update("new note");

            await storage.SaveNote(note, _userId);

            var fromDb = await storage.GetNote(note.State.Id, _userId);
            
            Assert.Equal("new note", fromDb.State.Note);

            await storage.Delete(_userId);

            var afterDelete = await storage.GetNotes(_userId);

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
        public async Task OwnedStock_Grading_Works()
        {
            var stock = new OwnedStock(GenerateTestTicker(), _userId.Item);

            stock.Purchase(10, 2.1m, DateTime.UtcNow);

            var storage = CreateStorage();

            await storage.Save(stock, _userId);

            var loaded = await storage.GetStock(stock.State.Ticker, _userId);

            loaded.Sell(10, 2.2m, DateTime.UtcNow, "sell");

            loaded.AssignGrade(0, TestDataGenerator.A, "test");

            await storage.Save(loaded, _userId);

            loaded = await storage.GetStock(loaded.State.Ticker, UserId.NewUserId(loaded.State.UserId));

            var position = loaded.State.GetAllPositions()[0];

            Assert.Equal(TestDataGenerator.A, position.Grade);
            Assert.Equal("test", position.GradeNote);

            var lastTx = position.Transactions.Last();

            loaded.DeleteTransaction(lastTx.TransactionId);

            await storage.Save(loaded, _userId);

            // make sure we can still load it
            _ = await storage.GetStock(loaded.State.Ticker, UserId.NewUserId(loaded.State.UserId));

            // clean up
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

            var loaded = await storage.GetRoutine(routine.State.Name, _userId);

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

            list.AddStock(TestDataGenerator.TSLA, "yeah yeah");

            await storage.SaveStockList(list, _userId);

            existing = await storage.GetStockLists(_userId);

            Assert.NotEmpty(existing);

            var loaded = await storage.GetStockList(list.State.Name, _userId);

            Assert.Equal(list.State.Name, loaded.State.Name);

            var ticker = loaded.State.Tickers.SingleOrDefault(t => t.Ticker.Equals(TestDataGenerator.TSLA));

            Assert.NotNull(ticker);

            await storage.DeleteStockList(loaded, _userId);

            existing = await storage.GetStockLists(_userId);

            Assert.Empty(existing);

            await storage.Delete(_userId);
        }

        [Fact]
        public async Task Notes_Works()
        {
            var storage = CreateStorage();
            
            var existing = await storage.GetNotes(_userId);
            
            Assert.Empty(existing);
            
            var note = new Note(
                _userId.Item,
                "description",
                TestDataGenerator.TSLA,
                DateTimeOffset.UtcNow
            );
            
            await storage.SaveNote(
                note,
                _userId
            );
            
            existing = await storage.GetNotes(_userId);
            
            Assert.NotEmpty(existing);
            
            var loaded = await storage.GetNote(
                note.State.Id,
                _userId
            );
            
            Assert.Equal(note.State.Note, loaded.State.Note);
            
            await storage.Delete(_userId);
            
            existing = await storage.GetNotes(_userId);
            
            Assert.Empty(existing);
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
                strategy: "strategy",
                ticker: TestDataGenerator.TSLA,
                userId: _userId.Item
            );

            await storage.SavePendingPosition(position, _userId);

            existing = await storage.GetPendingStockPositions(_userId);

            Assert.Single(existing);

            var loaded = existing.Single(p => p.State.Ticker.Equals(TestDataGenerator.TSLA));

            Assert.Equal(position.State.Id, loaded.State.Id);

            loaded.Purchase(price: 2.2m);

            await storage.SavePendingPosition(loaded, _userId);

            existing = await storage.GetPendingStockPositions(_userId);

            Assert.NotEmpty(existing);

            loaded = existing.Single(p => p.State.Ticker.Equals(TestDataGenerator.TSLA));

            Assert.True(loaded.State.IsClosed);
            Assert.NotNull(loaded.State.Closed);
            Assert.True(loaded.State.Purchased);

            await storage.Delete(_userId);
        }
        
    }
}