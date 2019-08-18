using System;
using System.Linq;
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

		public AnalysisController(IActorRef coordinator)
		{
			_coordinator = coordinator;
		}

		[Route("start")]
		public async Task<object> Start(int maxCost)
		{
			var jobId = await _coordinator.Ask<string>(new AnalyzeStocks(maxCost, 1.1f), TimeSpan.FromSeconds(15));

			return new { jobId };
		}

		[Route("status")]
		public async Task<object> StatusAsync(string jobId)
		{
			var r = await this._coordinator.Ask<JobStatus>(new JobStatusQuery(jobId), TimeSpan.FromSeconds(15));

			return r;
		}
	}
}