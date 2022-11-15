using System.Collections.Generic;
using System.Threading.Tasks;
using core.Shared;

namespace core.Adapters.Stocks
{
    public interface IStocksService2
    {
        Task<ServiceResponse<CompanyProfile>> GetCompanyProfile(string ticker);
        Task<ServiceResponse<StockAdvancedStats>> GetAdvancedStats(string ticker);
        Task<ServiceResponse<Price>> GetPrice(string ticker);
        Task<ServiceResponse<Dictionary<string, BatchStockPrice>>> GetPrices(IEnumerable<string> tickers);
        Task<ServiceResponse<List<SearchResult>>> Search(string fragment, int maxResults);
    }
}