using System.Threading.Tasks;
using financialmodelingclient;

namespace analysis
{
	public interface IAnalysisStorage
	{
		Task SaveAnalysisAsync(
			string symbol,
			MetricsResponse metrics,
			HistoricalResponse prices,
			CompanyProfile company
		);
	}
}