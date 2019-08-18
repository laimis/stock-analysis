using System;
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
		private IAnalysisStorage _storage;

		public AnalysisWorker(
			StocksService stock,
			string jobId,
			AnalyzeStocks cmd,
			IAnalysisStorage storage)
		{
			this._service = stock;
			this._jobId = jobId;
			this._cmd = cmd;
			this._storage = storage;

			ReceiveAsync<StartAnalysis>(m => Start(m));
			Receive<JobStatusQuery>(m => TellAboutStatus(m));
			ReceiveAsync<AnalyzeStock>(m => RunAnalysisAsync(m));
		}

		private async Task RunAnalysisAsync(AnalyzeStock m)
		{
			Console.WriteLine("Run analysis");

			if (_toAnalyze.Count == 0)
			{
				TellFinished();

				Context.Self.Tell(PoisonPill.Instance);

				return;
			}

			var symbol = _toAnalyze.Dequeue();

			var metrics = await this._service.GetKeyMetrics(symbol);
			var prices = await this._service.GetHistoricalDataAsync(symbol);
			var company = await this._service.GetCompanyProfile(symbol);

			await this._storage.SaveAnalysisAsync(symbol, metrics, prices, company);

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
			Console.WriteLine("Telling about finished signal");

			Context.Parent.Tell(new AnalysisFinished(this._jobId, CreateStatus()));
		}
	}
}