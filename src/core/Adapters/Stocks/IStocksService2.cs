using System.Threading.Tasks;

namespace core.Adapters.Stocks
{
    public interface IStocksService2
    {
        Task<CompanyProfile2> GetCompanyProfile(string ticker);
        Task<StockAdvancedStats> GetAdvancedStats(string ticker);
        Task<TickerPrice> GetPrice(string ticker);
    }
}