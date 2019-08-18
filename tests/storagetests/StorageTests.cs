using financialmodelingclient;
using storage;
using Xunit;

namespace storagetests
{
	public class StorageTests
	{
		[Fact]
		public async System.Threading.Tasks.Task StoraAnalysisAsync()
		{
			var service = new StocksService();

			var storage = new AnalysisStorage();

			var ticker = "CCL";

			var metrics = await service.GetKeyMetrics(ticker);
			var prices = await service.GetHistoricalDataAsync(ticker);
			var company = await service.GetCompanyProfile(ticker);
			
			await storage.SaveAnalysisAsync(
				ticker, metrics, prices, company
			);
		}
	}
}
