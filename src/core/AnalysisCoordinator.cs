using Akka.Actor;
using financialmodelingclient;

namespace core
{
	public class AnalysisCoordinator : ReceiveActor
	{
		private StocksService _stocks;
		private IAnalysisStorage _storage;
		private IActorRef _worker;

		public AnalysisCoordinator(StocksService stocks, IAnalysisStorage storage)
		{
			this._stocks = stocks;
			this._storage = storage;
			
			this.ReceiveAsync<AnalyzeStocks>(m => StartAnalysisAsync(m));

			var props = Props.Create(() => new AnalysisWorker(_stocks, _storage));

			this._worker = Context.ActorOf(props, "worker");
		}

		private async System.Threading.Tasks.Task StartAnalysisAsync(AnalyzeStocks m)
		{
			var stocks = await _stocks.GetAvailableStocks();

			foreach(var s in stocks.FilteredList)
			{
				if (s.Price >= m.MinPrice && s.Price <= m.MaxPrice)
				{
					_worker.Tell(new AnalyzeStock(s.Symbol));
				}
			}
		}
	}
}
