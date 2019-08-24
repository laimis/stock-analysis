using System.Linq;
using financialmodelingclient;
using storage;
using Xunit;

namespace storagetests
{
	[Trait("Category", "Integration")]
	public class StorageTests
	{
		private static string _cnn = "Server=localhost;Database=stocks;User id=stocks;password=stocks";
		
		[Fact]
		public async System.Threading.Tasks.Task StoraAnalysisAsync()
		{
			var service = new StocksService();

			var storage = new AnalysisStorage(_cnn);

			var ticker = "CCL";

			var metrics = await service.GetKeyMetrics(ticker);
			var prices = await service.GetHistoricalDataAsync(ticker);
			var company = await service.GetCompanyProfile(ticker);
			
			await storage.SaveAnalysisAsync(
				ticker, metrics, prices, company
			);

			var list = await storage.GetAnalysisAsync();

			Assert.NotEmpty(list);

			Assert.Equal("Health Care Providers", list.First().Industry);

			var a = await storage.GetAnalysisAsync("WHFBL");

			Assert.NotNull(a);
		}
	}
}
