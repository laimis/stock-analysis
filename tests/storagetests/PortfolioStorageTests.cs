using System;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Cryptos;
using core.Notes;
using core.Options;
using core.Portfolio;
using core.Shared;
using core.Stocks;
using Xunit;
using Xunit.Abstractions;

namespace storage.tests
{
    public abstract class PortfolioStorageTests
    {
        private static readonly Guid _userId = Guid.NewGuid();
        private readonly ITestOutputHelper _output;

        public PortfolioStorageTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task NonExistingStockReturnsNullAsync()
        {
            var storage = CreateStorage();

            Assert.Null(await storage.GetStock("nonexisting", _userId));
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
            var stock = new OwnedStock(GenerateTestTicker(), _userId);

            stock.Purchase(10, 2.1m, DateTime.UtcNow);

            var storage = CreateStorage();

            await storage.Save(stock, _userId);

            var loadedList = await storage.GetStocks(_userId);

            Assert.NotEmpty(loadedList);

            var loaded = await storage.GetStock(stock.State.Ticker, _userId);

            Assert.Equal(loaded.State.OpenPosition.NumberOfShares, stock.State.OpenPosition.NumberOfShares);

            loaded.Purchase(5, 5, DateTime.UtcNow);

            await storage.Save(loaded, _userId);

            loaded = await storage.GetStock(loaded.State.Ticker, loaded.State.UserId);

            Assert.NotEqual(loaded.State.OpenPosition.NumberOfShares, stock.State.OpenPosition.NumberOfShares);

            await storage.Delete(_userId);

            var afterDelete = await storage.GetStocks(_userId);

            Assert.Empty(afterDelete);
        }

        private static string GenerateTestTicker()
        {
            return $"test-{Guid.NewGuid().ToString("N")[..4]}";
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
                _userId
            );

            var storage = CreateStorage();

            await storage.Save(option, _userId);

            var loaded = await storage.GetOwnedOption(
                option.State.Id,
                _userId);

            Assert.NotNull(loaded);

            Assert.Equal(option.State.StrikePrice, loaded.State.StrikePrice);

            var list = await storage.GetOwnedOptions(_userId);

            var fromList = list.Single(o => o.State.Ticker == option.State.Ticker);

            Assert.Equal(option.State.StrikePrice, fromList.State.StrikePrice);

            await storage.Delete(_userId);

            var afterDelete = await storage.GetOwnedOptions(_userId);

            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task NoteStorageWorksAsync()
        {
            var note = new Note(_userId, "note", "tsla", DateTimeOffset.UtcNow);

            var storage = CreateStorage();

            await storage.Save(note, _userId);

            var notes = await storage.GetNotes(_userId);

            Assert.NotEmpty(notes);

            note = notes.Single(n => n.State.Id == note.State.Id);

            note.Update("new note");

            await storage.Save(note, _userId);

            var fromDb = await storage.GetNote(_userId, note.State.Id);
            
            Assert.Equal("new note", fromDb.State.Note);

            await storage.Delete(_userId);

            var afterDelete = await storage.GetNotes(_userId);

            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task OwnedCryptoWorksAsync()
        {
            var crypto = new OwnedCrypto(new Token("BTC"), _userId);

            crypto.Purchase(10, 2.1m, DateTimeOffset.UtcNow);

            var storage = CreateStorage();

            await storage.Save(crypto, _userId);

            var loadedList = await storage.GetCryptos(_userId);

            Assert.NotEmpty(loadedList);

            var loaded = await storage.GetCrypto(crypto.State.Token, _userId);

            Assert.Equal(loaded.State.Quantity, crypto.State.Quantity);

            loaded.Purchase(5, 5, DateTime.UtcNow);

            await storage.Save(loaded, _userId);

            loaded = await storage.GetCrypto(loaded.State.Token, loaded.State.UserId);

            Assert.NotEqual(loaded.State.Quantity, crypto.State.Quantity);

            await storage.Delete(_userId);

            var afterDelete = await storage.GetCryptos(_userId);

            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task OwnedStock_Grading_Works()
        {
            var stock = new OwnedStock(GenerateTestTicker(), _userId);

            stock.Purchase(10, 2.1m, DateTime.UtcNow);

            var storage = CreateStorage();

            await storage.Save(stock, _userId);

            var loaded = await storage.GetStock(stock.State.Ticker, _userId);

            loaded.Sell(10, 2.2m, DateTime.UtcNow, "sell");

            loaded.AssignGrade(0, "A", "test");

            await storage.Save(loaded, _userId);

            loaded = await storage.GetStock(loaded.State.Ticker, loaded.State.UserId);

            var position = loaded.State.GetAllPositions()[0];

            Assert.Equal("A", position.Grade);
            Assert.Equal("test", position.GradeNote);

            var lastTx = position.Transactions.Last();

            loaded.DeleteTransaction(lastTx.transactionId);

            await storage.Save(loaded, _userId);

            // make sure we can still load it
            _ = await storage.GetStock(loaded.State.Ticker, loaded.State.UserId);

            // clean up
            await storage.Delete(_userId);    
        }

        [Fact]
        public async Task Routines_Works()
        {
            var storage = CreateStorage();

            var existing = await storage.GetRoutines(_userId);

            Assert.Empty(existing);

            var routine = new Routine("description", "name", _userId);

            routine.AddStep("step", "url");

            await storage.Save(routine, _userId);

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

            var list = new StockList("description", "name", _userId);

            list.AddStock("tsla", "yeah yeah");

            await storage.Save(list, _userId);

            existing = await storage.GetStockLists(_userId);

            Assert.NotEmpty(existing);

            var loaded = await storage.GetStockList(list.State.Name, _userId);

            Assert.Equal(list.State.Name, loaded.State.Name);

            var ticker = loaded.State.Tickers.SingleOrDefault(t => t.Ticker == new Ticker("tsla"));

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
                strategy: "strategy",
                ticker: "tsla",
                userId: _userId
            );

            await storage.Save(position, _userId);

            existing = await storage.GetPendingStockPositions(_userId);

            Assert.Single(existing);

            var loaded = existing.Single(p => p.State.Ticker == "TSLA");

            Assert.Equal(position.State.Id, loaded.State.Id);

            loaded.Close(purchased: true, price: 2.2m);

            await storage.Save(loaded, _userId);

            existing = await storage.GetPendingStockPositions(_userId);

            Assert.NotEmpty(existing);

            loaded = existing.Single(p => p.State.Ticker == "TSLA");

            Assert.True(loaded.State.IsClosed);
            Assert.NotNull(loaded.State.Closed);
            Assert.True(loaded.State.Purchased);

            await storage.Delete(_userId);
        }
        
    }
}