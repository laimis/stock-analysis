using System;
using System.Threading.Tasks;
using Akka.Actor;
using financialmodelingclient;

namespace analysis
{
	public class AnalysisWorker : ReceiveActor
	{
		private StocksService _service;
		private IAnalysisStorage _storage;

		public AnalysisWorker(
			StocksService stock,
			IAnalysisStorage storage)
		{
			this._service = stock;
			this._storage = storage;

			ReceiveAsync<AnalyzeStock>(m => RunAnalysisAsync(m));
		}

		private async Task RunAnalysisAsync(AnalyzeStock m)
		{
			Console.WriteLine("Run analysis " + m.Ticker);

			var ticker = m.Ticker;

			var metrics = await this._service.GetKeyMetrics(ticker);
			var prices = await this._service.GetHistoricalDataAsync(ticker);
			var company = await this._service.GetCompanyProfile(ticker);

			await this._storage.SaveAnalysisAsync(ticker, metrics, prices, company);
		}
	}
}