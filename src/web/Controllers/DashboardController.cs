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

		[HttpGet("portfolio")]
		public string[] Portfolio()
		{
			return new [] {
				"F",
				"CTST",
				"EGBN",
				"BAC",
				"ACB",
				"IRBT",
				"TRQ",
				"TEUM",
				"FTSV"
			};
		}
	}
}