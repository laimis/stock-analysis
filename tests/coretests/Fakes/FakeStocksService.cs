using System.Collections.Generic;
using System.Threading.Tasks;
using core;
using core.Adapters.Stocks;

namespace coretests.Fakes
{
    internal class FakeStocksService : IStocksService2
    {
        private StockQueryResult _active;
        private StockQueryResult _gainer;
        private StockQueryResult _loser;

        public FakeStocksService()
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

        public Task<CompanyProfile> GetCompanyProfile(string ticker)
        {
            throw new System.NotImplementedException();
        }

        public Task<StockAdvancedStats> GetAdvancedStats(string ticker)
        {
            throw new System.NotImplementedException();
        }

        public Task<TickerPrice> GetPrice(string ticker)
        {
            return Task.FromResult(new TickerPrice());
        }

        public Task<List<SearchResult>> Search(string fragment)
        {
            return Task.FromResult(new List<SearchResult>());
        }

        public Task<Dictionary<string, BatchStockPrice>> GetPrices(IEnumerable<string> tickers)
        {
            throw new System.NotImplementedException();
        }
    }
}