using System;
using System.Linq;
using core.Stocks;
using testutils;
using Xunit;

namespace coretests.Stocks
{
    public class StockListTests
    {
        private static Guid _userId = Guid.NewGuid();

        private static StockList Create(string name, string description) =>
            new(description: description, name: name, userId: _userId);
        
        [Fact]
        public void CreateWorks()
        {
            var name = "hunt";
            var description = "description";
            
            var list = Create(name, description);

            var state = list.State;

            Assert.NotEqual(Guid.Empty, state.Id);
            Assert.Equal(_userId, state.UserId);
            Assert.Equal(description, state.Description);
            Assert.Equal(name, state.Name);
        }

        [Fact]
        public void UpdateWorks()
        {
            var list = Create("name", "description");

            list.Update(name: "new name", description: "new description");

            Assert.Equal("new name", list.State.Name);
            Assert.Equal("new description", list.State.Description);
        }

        [Fact]
        public void CreateWithNoNameFails()
        {
            Assert.Throws<InvalidOperationException>(() => Create("", "description"));
        }

        [Fact]
        public void CreateWithNoDescriptionWorks()
        {
            var list = Create("name", null);

            Assert.NotNull(list);
        }

        [Fact]
        public void AddingTickerWorks()
        {
            var list = Create("name", "description");

            list.AddStock(TestDataGenerator.AMD, "note");

            Assert.Single(list.State.Tickers);
            Assert.Equal(TestDataGenerator.AMD, list.State.Tickers[0].Ticker);
        }

        [Fact]
        public void AddingTickerWithNoNoteWorks()
        {
            var list = Create("name", "description");

            list.AddStock(TestDataGenerator.AMD, null);

            Assert.Single(list.State.Tickers);
            Assert.Equal(TestDataGenerator.AMD, list.State.Tickers[0].Ticker);
        }

        [Fact]
        public void RemovingTickerWorks()
        {
            var list = Create("name", "description");

            list.AddStock(TestDataGenerator.AMD, "note");

            Assert.Single(list.State.Tickers);

            list.RemoveStock(TestDataGenerator.AMD);

            Assert.Empty(list.State.Tickers);
        }
        
        [Fact]
        public void RemovingTickerThatDoesNotExistFails()
        {
            var list = Create("name", "description");

            Assert.Throws<InvalidOperationException>(() => list.RemoveStock(TestDataGenerator.AMD));
        }

        [Fact]
        public void AddingTickerThatAlreadyExistsIsNoOp()
        {
            var list = Create("name", "description");

            list.AddStock(TestDataGenerator.AMD, "note");

            var events = list.Events.Count();

            list.AddStock(TestDataGenerator.AMD, "note");
            list.AddStock(TestDataGenerator.AMD, "note");


            Assert.Single(list.State.Tickers);
            Assert.Equal(events, list.Events.Count());
        }

        [Fact]
        public void AddTagWorks()
        {
            var list = Create("name", "description");

            list.AddTag("tag");
            list.AddTag("tag");

            Assert.Single(list.State.Tags);
            Assert.Equal("tag", list.State.Tags.First());
        }

        [Fact]
        public void AddInvalidTagFails()
        {
            var list = Create("name", "description");

            Assert.Throws<InvalidOperationException>(() => list.AddTag(""));
        }

        [Fact]
        public void RemoveTagWorks()
        {
            var list = Create("name", "description");

            list.AddTag("tag");
            list.RemoveTag("tag");

            Assert.Empty(list.State.Tags);
        }

        [Fact]
        public void RemoveTagThatDoesNotExistIsNoOp()
        {
            var list = Create("name", "description");

            list.RemoveTag("tag");

            Assert.Empty(list.State.Tags);
        }

        [Fact]
        public void AddStocksWhenClear_ClearsList()
        {
            var list = Create("name", "description");
            
            list.AddStock(TestDataGenerator.NET, "note");
            list.AddStock(TestDataGenerator.AMD, "note");
            
            list.Clear();
            
            Assert.Empty(list.State.Tickers);
        }
    }
}
