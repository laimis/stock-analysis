using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
	[Route("api/[controller]")]
	public class DashboardController : Controller
	{
		public DashboardController()
		{
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