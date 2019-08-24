using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using analysis;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
	[Route("api/[controller]")]
	public class AnalysisController : Controller
	{
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

		[HttpPost("start")]
		public void Start(int min, int max)
		{
			_coordinator.Tell(new AnalyzeStocks(min, max));
		}

		[HttpGet()]
		public async Task<object> ListAsync(string sortBy, string sortDirection)
		{
			var list = await this._storage.GetAnalysisAsync();

			list = list.Where(a => a.LastPEValue > 0);

			if (sortBy != null)
			{
				if (sortDirection == "asc")
				{
					list = list.OrderBy(a => GetSortValue(a, sortBy));
				}
				else
				{
					list = list.OrderByDescending(a => GetSortValue(a, sortBy));
				}
			}

			return list;
		}

		private object GetSortValue(AnalysisInfo a, string sortBy)
		{
			if (sortBy == "price")
			{
				return a.LastPrice;
			}

			return a.Ticker;
		}
	}
}