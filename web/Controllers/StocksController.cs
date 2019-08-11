using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using web.Services;

namespace web.Controllers
{
	[Route("api/[controller]")]
	public class StocksController : Controller
	{
		private StockService _stockService;

		public StocksController(StockService stockService)
		{
			_stockService = stockService;
		}
		
		[HttpGet("{ticker}")]
		public async Task<object> SummaryAsync(string ticker)
		{
			var data = await _stockService.GetHistoricalDataAsync(ticker);

			var price = data.Historical.Last().Close;

			var largestGain = data.Historical.Max(p => p.ChangePercent);
			var largestLoss = data.Historical.Min(p => p.ChangePercent);

			var byMonth = data.Historical.GroupBy(r => r.Date.ToString("yyyy-MM-01"))
				.Select(g => new {
					Date = DateTime.Parse(g.Key),
					Price = g.Average(p => p.Close),
					Volume = g.Average(p => p.Volume)
				});

			var priceLabels = byMonth.Select(a => a.Date.ToString("MMMM"));
			var priceValues = byMonth.Select(a => Math.Round(a.Price,2));
			var volumeValues = byMonth.Select(a => a.Volume);

			var metrics = await _stockService.GetKeyMetrics(ticker);

			var mostRecent = metrics.Metrics[0];

			var age = (int)(DateTime.UtcNow.Subtract(mostRecent.Date).TotalDays / 30);

			return new
			{
				price,
				largestGain,
				largestLoss,
				priceLabels,
				priceValues,
				volumeValues,
				age,
				bookValue = mostRecent.BookValuePerShare,
				peValue = mostRecent.PERatio,
				bookValues = metrics.Metrics.Select(m => m.BookValuePerShare),
				peValues = metrics.Metrics.Select(m => m.PERatio)
			};
		}
	}
}
