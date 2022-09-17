using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Adapters.Stocks
{
    public interface IStocksService2
    {
        Task<ServiceResponse<CompanyProfile>> GetCompanyProfile(string ticker);
        Task<ServiceResponse<StockAdvancedStats>> GetAdvancedStats(string ticker);
        Task<ServiceResponse<Price>> GetPrice(string ticker);
        Task<ServiceResponse<Dictionary<string, BatchStockPrice>>> GetPrices(IEnumerable<string> tickers);
        Task<ServiceResponse<List<SearchResult>>> Search(string fragment, int maxResults);
        Task<ServiceResponse<Quote>> Quote(string ticker);
    }
}