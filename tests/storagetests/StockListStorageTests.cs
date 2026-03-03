using System;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Adapters.Storage;
using core.fs.Accounts;
using core.fs.Stocks;
using Microsoft.FSharp.Core;
using testutils;
using Xunit;

namespace storagetests
{
    public abstract class StockListStorageTests
    {
        private static readonly UserId _userId = UserId.NewUserId(Guid.NewGuid());

        protected abstract IStockListStorage CreateStorage();

        [Fact]
        public async Task StockList_Works()
        {
            var storage = CreateStorage();

            // initially empty
            var existing = await storage.GetStockLists(_userId);
            Assert.Empty(existing);

            // create a list
            var created = await storage.SaveStockList(FSharpOption<Guid>.None, "mylist", "my description", _userId);
            Assert.True(FSharpOption<StockList>.get_IsSome(created));
            var list = created.Value;
            Assert.NotEqual(Guid.Empty, list.Id);
            Assert.Equal("mylist", list.Name);
            Assert.Equal("my description", list.Description);
            Assert.Empty(list.Tickers);
            Assert.Empty(list.Tags);

            // get all lists
            existing = await storage.GetStockLists(_userId);
            Assert.Single(existing);

            // get single list
            var loaded = await storage.GetStockList(list.Id, _userId);
            Assert.True(FSharpOption<StockList>.get_IsSome(loaded));
            Assert.Equal(list.Name, loaded.Value.Name);

            // update list
            var updated = await storage.SaveStockList(FSharpOption<Guid>.Some(list.Id), "renamed", "new desc", _userId);
            Assert.True(FSharpOption<StockList>.get_IsSome(updated));
            Assert.Equal("renamed", updated.Value.Name);
            Assert.Equal("new desc", updated.Value.Description);

            // add ticker
            var withTicker = await storage.AddTickerToStockList(list.Id, TestDataGenerator.AMD, "note", _userId);
            Assert.True(FSharpOption<StockList>.get_IsSome(withTicker));
            Assert.Single(withTicker.Value.Tickers);
            Assert.Equal(TestDataGenerator.AMD, withTicker.Value.Tickers.First().Ticker);

            // adding same ticker again is a no-op
            var withTickerAgain = await storage.AddTickerToStockList(list.Id, TestDataGenerator.AMD, "note", _userId);
            Assert.Single(withTickerAgain.Value.Tickers);

            // add tag
            var withTag = await storage.AddTagToStockList(list.Id, "monitor", _userId);
            Assert.True(FSharpOption<StockList>.get_IsSome(withTag));
            Assert.Single(withTag.Value.Tags);
            Assert.Equal("monitor", withTag.Value.Tags.First());

            // adding same tag again is a no-op
            var withTagAgain = await storage.AddTagToStockList(list.Id, "monitor", _userId);
            Assert.Single(withTagAgain.Value.Tags);

            // remove tag
            var withoutTag = await storage.RemoveTagFromStockList(list.Id, "monitor", _userId);
            Assert.True(FSharpOption<StockList>.get_IsSome(withoutTag));
            Assert.Empty(withoutTag.Value.Tags);

            // add ticker back, then clear
            await storage.AddTickerToStockList(list.Id, TestDataGenerator.NET, null, _userId);
            var cleared = await storage.ClearStockListTickers(list.Id, _userId);
            Assert.True(FSharpOption<StockList>.get_IsSome(cleared));
            Assert.Empty(cleared.Value.Tickers);

            // add ticker back, then remove it individually
            await storage.AddTickerToStockList(list.Id, TestDataGenerator.AMD, null, _userId);
            var withoutTicker = await storage.RemoveTickerFromStockList(list.Id, TestDataGenerator.AMD, _userId);
            Assert.True(FSharpOption<StockList>.get_IsSome(withoutTicker));
            Assert.Empty(withoutTicker.Value.Tickers);

            // delete list
            await storage.DeleteStockList(list.Id, _userId);
            existing = await storage.GetStockLists(_userId);
            Assert.Empty(existing);

            // cleanup
            await storage.DeleteAllStockLists(_userId);
        }

        [Fact]
        public async Task GetStockList_NonExistent_ReturnsNone()
        {
            var storage = CreateStorage();

            var result = await storage.GetStockList(Guid.NewGuid(), _userId);

            Assert.True(FSharpOption<StockList>.get_IsNone(result));
        }

        [Fact]
        public async Task StockList_OtherUser_CannotAccess()
        {
            var storage = CreateStorage();
            var otherUserId = UserId.NewUserId(Guid.NewGuid());

            var created = await storage.SaveStockList(FSharpOption<Guid>.None, "private list", "", _userId);
            var list = created.Value;

            var result = await storage.GetStockList(list.Id, otherUserId);
            Assert.True(FSharpOption<StockList>.get_IsNone(result));

            // cleanup
            await storage.DeleteAllStockLists(_userId);
        }
    }
}
