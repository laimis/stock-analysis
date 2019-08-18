using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using financialmodelingclient;

namespace analysis
{
	public class AnalysisWorker : ReceiveActor
	{
		private StocksService _service;
		private string _jobId;

		private List<string> _analyzed = new List<string>();
		private Queue<string> _toAnalyze = new Queue<string>();
		private List<string> _candidates = new List<string>();
		private AnalyzeStocks _cmd;

		public AnalysisWorker(StocksService stock, string jobId, AnalyzeStocks cmd)
		{
			this._service = stock;
			this._jobId = jobId;
			this._cmd = cmd;

			ReceiveAsync<StartAnalysis>(m => Start(m));
			Receive<JobStatusQuery>(m => TellAboutStatus(m));
			ReceiveAsync<AnalyzeStock>(m => RunAnalysisAsync(m));
		}

		private async Task RunAnalysisAsync(AnalyzeStock m)
		{
			if (_toAnalyze.Count == 0)
			{
				TellFinished();
			}

			var symbol = _toAnalyze.Dequeue();

			var metrics = await this._service.GetKeyMetrics(symbol);

			var mostRecent = metrics.Metrics.FirstOrDefault();

			if (mostRecent != null && mostRecent.BookValuePerShare.HasValue)
			{
				var prices = await this._service.GetHistoricalDataAsync(symbol);

				if (prices.Historical.Length > 0)
				{
					var price = prices.Historical.Last().Close;

					if (price <= mostRecent.BookValuePerShare.Value * _cmd.BookValuePremium)
					{
						_candidates.Add(symbol);
					}
				}
			}

			_analyzed.Add(symbol);

			Self.Tell(new AnalyzeStock());
		}

		private void TellAboutStatus(JobStatusQuery m)
		{
			this.Sender.Tell(CreateStatus());
		}

		private JobStatus CreateStatus()
		{
			return new JobStatus( _analyzed.Count, _toAnalyze.Count, _candidates.ToArray(), this._cmd);
		}

		private async Task Start(StartAnalysis m)
		{
			var stocks = await this._service.GetAvailableStocks();

			var candidates = stocks.FilteredList.Where(s => s.Price <= _cmd.PriceLevel);

			var symbols = candidates.Select(s => s.Symbol).Take(20).ToArray();

			_toAnalyze = new Queue<string>(symbols);

			_analyzed = new List<string>();

			this.Self.Tell(new AnalyzeStock());
		}

		private void TellFinished()
		{
			Context.Parent.Tell(new AnalysisFinished(this._jobId, CreateStatus()));
		}
	}
}