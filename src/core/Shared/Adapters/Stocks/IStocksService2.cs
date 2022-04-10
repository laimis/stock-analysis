using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Adapters.Stocks
{
    public interface IStocksService2
    {
        Task<StockServiceResponse<CompanyProfile, string>> GetCompanyProfile(string ticker);
        Task<StockServiceResponse<StockAdvancedStats, string>> GetAdvancedStats(string ticker);
        Task<StockServiceResponse<Price, string>> GetPrice(string ticker);
        Task<StockServiceResponse<Dictionary<string, BatchStockPrice>, string>> GetPrices(IEnumerable<string> tickers);
        
        Task<StockServiceResponse<List<SearchResult>,string>> Search(string fragment, int maxResults);
        Task<StockServiceResponse<Quote,string>> Quote(string ticker);
    }
}