using System;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Notes;
using core.Options;
using core.Stocks;
using Xunit;

namespace storage.tests
{
    public abstract class PortfolioStorageTests
    {
        const string _userId = "testuser";

        [Fact]
        public async Task NonExistingStockReturnsNullAsync()
        {
            var storage = CreateStorage();

            Assert.Null(await storage.GetStock("nonexisting", "nonexisting"));
        }

        protected abstract IPortfolioStorage CreateStorage();

        [Fact]
        public async Task NonExistingOptionReturnsNullAsync()
        {
            var storage = CreateStorage();

            Assert.Null(await storage.GetSoldOption(Guid.NewGuid(), _userId));
        }

        [Fact]
        public async Task OwnedStockStorageAsync()
        {
            var stock = new OwnedStock(GenerateTestTicker(), _userId);

            stock.Purchase(10, 2.1, DateTime.UtcNow);

            var storage = CreateStorage();

            await storage.Save(stock, _userId);

            var loadedList = await storage.GetStocks(_userId);

            Assert.NotEmpty(loadedList);

            var loaded = await storage.GetStock(stock.State.Ticker, _userId);

            Assert.Equal(loaded.State.Owned, stock.State.Owned);

            loaded.Purchase(5, 5, DateTime.UtcNow);

            await storage.Save(loaded, _userId);

            loaded = await storage.GetStock(loaded.State.Ticker, loaded.State.UserId);

            Assert.NotEqual(loaded.State.Owned, stock.State.Owned);
        }

        private static string GenerateTestTicker()
        {
            return $"test-{Guid.NewGuid().ToString("N").Substring(0, 4)}";
        }

        [Fact]
        public async Task OwnedOption_WorksAsync()
        {
            var expiration = DateTimeOffset.UtcNow.AddDays(30).Date;

            var option = new SoldOption(
                GenerateTestTicker(),
                OptionType.CALL,
                expiration,
                2.5,
                _userId,
                1,
                8,
                DateTimeOffset.UtcNow
            );

            var storage = CreateStorage();

            await storage.Save(option, _userId);

            var loaded = await storage.GetSoldOption(
                option.State.Id,
                _userId);

            Assert.NotNull(loaded);

            Assert.Equal(option.State.StrikePrice, loaded.State.StrikePrice);

            var list = await storage.GetSoldOptions(_userId);

            var fromList = list.Single(o => o.State.Ticker == option.State.Ticker);

            Assert.Equal(option.State.StrikePrice, fromList.State.StrikePrice);
        }

        [Fact]
        public async Task NoteStorageWorksAsync()
        {
            var note = new Note(_userId, "note", "ticker", null, DateTimeOffset.UtcNow);

            var storage = CreateStorage();

            await storage.Save(note, _userId);

            var notes = await storage.GetNotes(_userId);

            Assert.NotEmpty(notes);

            note = notes.Single(n => n.State.Id == note.State.Id);

            note.Update("new note", 100);

            await storage.Save(note, _userId);

            var fromDb = await storage.GetNote(_userId, note.State.Id);
            
            Assert.Equal("new note", fromDb.State.Note);
        }
    }
}