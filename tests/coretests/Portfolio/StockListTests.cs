using System;
using core.Portfolio;
using Xunit;

namespace coretests.Stocks
{
    public class StockListTests
    {
        private static Guid _userId = Guid.NewGuid();

        private static StockList Create(string name, string description) =>
            new StockList(description: description, name: name, userId: _userId);
        
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
    }
}
