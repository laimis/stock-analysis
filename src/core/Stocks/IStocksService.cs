using System.Threading.Tasks;

namespace core.Stocks
{
	public interface IStocksService
	{
		Task<StockListResponse> GetAvailableStocks();
		Task<MetricsResponse> GetKeyMetrics(string ticker);
		Task<HistoricalResponse> GetHistoricalDataAsync(string ticker);
		Task<CompanyProfile> GetCompanyProfile(string ticker);
		Task<StockRatings> GetRatings(string ticker);
	}
}