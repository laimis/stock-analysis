using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Stocks;
using Xunit;

namespace coretests.Stocks
{
    public class StockListsTests
    {
        [Fact]
        public async Task MostActive_UsesService()
        {
            await EnsureRightListIsCalledAsync(
                (l, r) => l.RegisterActive(r),
                h => h.Handle(new StockLists.QueryMostActive(), CancellationToken.None)
            );
        }

        [Fact]
        public async Task Gainers_UsesService()
        {
            await EnsureRightListIsCalledAsync(
                (l, r) => l.RegisterGainer(r),
                h => h.Handle(new StockLists.QueryGainers(), CancellationToken.None)
            );
        }

        [Fact]
        public async Task Losers_UsesService()
        {
            await EnsureRightListIsCalledAsync(
                (l, r) => l.RegisterLoser(r),
                h => h.Handle(new StockLists.QueryLosers(), CancellationToken.None)
            );
        }

        private async Task EnsureRightListIsCalledAsync(
            Action<Fakes.FakeStocksLists, StockQueryResult> registerAction,
            Func<StockLists.Handler, Task<List<StockQueryResult>>> func)
        {
            var lists = new Fakes.FakeStocksLists();

            registerAction(lists, new StockQueryResult());
            
            var handler = new StockLists.Handler(lists);

            var result = await func(handler);

            Assert.NotEmpty(result);
        }
    }
}