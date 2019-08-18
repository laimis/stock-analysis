using System;
using System.Threading.Tasks;
using Akka.Actor;
using analysis;
using financialmodelingclient;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
	[Route("api/[controller]")]
	public class AnalysisController : Controller
	{
		private StocksService _stocksService;
		private IActorRef _coordinator;
		private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(15);

		public AnalysisController(IActorRef coordinator)
		{
			_coordinator = coordinator;
		}

		[Route("start")]
		public async Task<object> Start(int maxCost)
		{
			var jobId = await _coordinator.Ask<string>(new AnalyzeStocks(maxCost, 1.1f), _timeout);

			return new {
				jobId,
				href = "/api/analysis/jobs/" + jobId
			};
		}

		[Route("jobs/{jobId}")]
		public async Task<object> JobStatusAsync(string jobId)
		{
			var r = await this._coordinator.Ask<JobStatus>(new JobStatusQuery(jobId), _timeout);

			return r;
		}

		[Route("jobs")]
		public async Task<object> Jobs()
		{
			var r = await this._coordinator.Ask<JobsStatus>(new JobsStatusQuery(), _timeout);

			return r;
		}
	}
}