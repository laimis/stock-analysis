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
		private IAnalysisStorage _storage;
		private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(15);

		public AnalysisController(
			IActorRef coordinator,
			IAnalysisStorage storage
		)
		{
			_coordinator = coordinator;
			_storage = storage;
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

		[HttpGet()]
		public object List()
		{
			return new { jobs = new object[0]};
		}
	}
}