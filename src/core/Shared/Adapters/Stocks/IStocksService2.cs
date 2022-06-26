using System.Collections.Generic;
using System.Threading.Tasks;
using core.Shared.Adapters.Stocks;

namespace core.Adapters.Stocks
{
    public interface IStocksService2
    {
        Task<StockServiceResponse<CompanyProfile>> GetCompanyProfile(string ticker);
        Task<StockServiceResponse<StockAdvancedStats>> GetAdvancedStats(string ticker);
        Task<StockServiceResponse<Price>> GetPrice(string ticker);
        Task<StockServiceResponse<Dictionary<string, BatchStockPrice>>> GetPrices(IEnumerable<string> tickers);

        Task<StockServiceResponse<HistoricalPrice[]>> GetHistoricalPrices(string ticker, string interval);
        
        Task<StockServiceResponse<List<SearchResult>>> Search(string fragment, int maxResults);
        Task<StockServiceResponse<Quote>> Quote(string ticker);
    }
}