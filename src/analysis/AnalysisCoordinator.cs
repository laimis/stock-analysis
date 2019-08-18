using System;
using System.Collections.Generic;
using Akka.Actor;
using financialmodelingclient;

namespace analysis
{
	public class AnalysisCoordinator : ReceiveActor
	{
		private Dictionary<string, IActorRef> _workers = new Dictionary<string, IActorRef>();
		private Dictionary<string, JobStatus> _results = new Dictionary<string, JobStatus>();
		private StocksService _stocks;

		public AnalysisCoordinator(StocksService stocks)
		{
			this._stocks = stocks;
			
			this.Receive<AnalyzeStocks>(m => StartAnalysis(m));
			this.Receive<AnalysisFinished>(m => AnalysisFinished(m));
			this.ReceiveAsync<JobStatusQuery>(m => JobStatusAsync(m));
		}

		private void StartAnalysis(AnalyzeStocks m)
		{
			var jobId = Guid.NewGuid().ToString("N");

			var props = Props.Create(() => new AnalysisWorker(_stocks, jobId, m));

			var actor = Context.ActorOf(props, jobId);

			_workers.Add(jobId, actor);

			actor.Tell(new StartAnalysis());

			this.Sender.Tell(jobId);
		}

		private void AnalysisFinished(AnalysisFinished m)
		{
			_workers.Remove(m.JobId);

			_results[m.JobId] = m.Status;
		}

		private async System.Threading.Tasks.Task JobStatusAsync(JobStatusQuery q)
		{
			if (_workers.ContainsKey(q.JobId))
			{
				var r = await _workers[q.JobId].Ask<JobStatus>(q);

				this.Sender.Tell(r);
			}
			else
			{
				this.Sender.Tell(_results[q.JobId]);
			}
		}
	}
}
