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
		private IAnalysisStorage _storage;

		public AnalysisCoordinator(StocksService stocks, IAnalysisStorage storage)
		{
			this._stocks = stocks;
			this._storage = storage;
			
			this.Receive<AnalyzeStocks>(m => StartAnalysis(m));
			this.Receive<AnalysisFinished>(m => AnalysisFinished(m));
			this.ReceiveAsync<JobStatusQuery>(m => JobStatusAsync(m));
			this.Receive<JobsStatusQuery>(m => JobsStatus(m));
		}

		private void JobsStatus(JobsStatusQuery m)
		{
			var jobs = new List<KeyValuePair<string, string>>();

			foreach(var k in _workers)
			{
				jobs.Add(new KeyValuePair<string, string>(k.Key, "inprocess"));
			}

			foreach(var k in _results)
			{
				jobs.Add(new KeyValuePair<string, string>(k.Key, "completed"));
			}

			Sender.Tell(new JobsStatus(jobs));
		}

		private void StartAnalysis(AnalyzeStocks m)
		{
			var jobId = Guid.NewGuid().ToString("N");

			var props = Props.Create(() => new AnalysisWorker(_stocks, jobId, m, _storage));

			var actor = Context.ActorOf(props, jobId);

			_workers.Add(jobId, actor);

			actor.Tell(new StartAnalysis());

			this.Sender.Tell(jobId);
		}

		private void AnalysisFinished(AnalysisFinished m)
		{
			Console.WriteLine("Received finished signal for job " + m.JobId);

			_workers.Remove(m.JobId);

			_results[m.JobId] = m.Status;
		}

		private async System.Threading.Tasks.Task JobStatusAsync(JobStatusQuery q)
		{
			if (_workers.ContainsKey(q.JobId))
			{
				Console.WriteLine("Asking worker about status");

				var r = await _workers[q.JobId].Ask<JobStatus>(q);

				this.Sender.Tell(r);
			}
			else
			{
				Console.WriteLine("Using results for status");

				this.Sender.Tell(_results[q.JobId]);
			}
		}
	}
}
