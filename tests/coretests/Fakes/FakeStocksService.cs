using System.Collections.Generic;
using System.Threading.Tasks;
using core;
using core.Adapters.Stocks;

namespace coretests.Fakes
{
    internal class FakeStocksService : IStocksService2
    {
        public FakeStocksService()
        {
        }

        private Task<List<StockQueryResult>> Get(StockQueryResult result)
        {
            return Task.FromResult(new List<StockQueryResult>{result});
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