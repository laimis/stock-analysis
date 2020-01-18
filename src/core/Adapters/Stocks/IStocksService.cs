using System.Threading.Tasks;

namespace core.Adapters.Stocks
{
	public interface IStocksService
	{
		Task<MetricsResponse> GetKeyMetrics(string ticker);
		Task<HistoricalResponse> GetHistoricalDataAsync(string ticker);
	}
}