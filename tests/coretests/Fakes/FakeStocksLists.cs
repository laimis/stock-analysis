using System.Collections.Generic;
using System.Threading.Tasks;
using core.Adapters.Stocks;

namespace coretests.Fakes
{
    internal class FakeStocksLists : IStocksLists
    {
        private StockQueryResult _active;
        private StockQueryResult _gainer;
        private StockQueryResult _loser;

        public FakeStocksLists()
        {
        }

        public Task<List<StockQueryResult>> GetMostActive()
        {
            return Get(_active);
        }

        public Task<List<StockQueryResult>> GetLosers()
        {
            return Get(_loser);
        }

        public Task<List<StockQueryResult>> GetGainers()
        {
            return Get(_gainer);
        }

        private Task<List<StockQueryResult>> Get(StockQueryResult result)
        {
            return Task.FromResult(new List<StockQueryResult>{result});
        }

        internal void RegisterActive(StockQueryResult entry)
        {
            _active = entry;
        }

        internal void RegisterGainer(StockQueryResult entry)
        {
            _gainer = entry;
        }

        internal void RegisterLoser(StockQueryResult entry)
        {
            _loser = entry;
        }
    }
}