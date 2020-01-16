using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Stocks;
using Xunit;

namespace coretests.Stocks
{
    public class MostActiveTests
    {
        [Fact]
        public async Task MostActive_UsesServiceAsync()
        {
            var query = new MostActive.Query();

            var lists = new Fakes.FakeStocksLists();

            lists.Register(new MostActiveEntry());
            
            var handler = new MostActive.Handler(lists);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotEmpty(result);
        }
    }
}