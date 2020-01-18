using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Adapters.Stocks
{
    public interface IStocksService2
    {
        Task<CompanyProfile> GetCompanyProfile(string ticker);
        Task<StockAdvancedStats> GetAdvancedStats(string ticker);
        Task<TickerPrice> GetPrice(string ticker);
        Task<List<StockQueryResult>> GetMostActive();
        Task<List<StockQueryResult>> GetGainers();
        Task<List<StockQueryResult>> GetLosers();
    }
}