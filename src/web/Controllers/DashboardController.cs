using System.Threading.Tasks;
using financialmodelingclient;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
	[Route("api/[controller]")]
	public class DashboardController : Controller
	{
		private StocksService _stocksService;

		public DashboardController(StocksService stockService)
		{
			_stocksService = stockService;
		}

		[HttpGet]
		public async Task<object> Index()
		{
			var active = await _stocksService.GetMostActive();
			var gainer = await _stocksService.GetMostGainer();
			var loser = await _stocksService.GetMostLosers();

			return new {
				active,
				gainer,
				loser
			};
		}
	}
}