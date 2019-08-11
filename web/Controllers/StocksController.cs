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
		private StocksService _stocksService;

		public StocksController(StocksService stockService)
		{
			_stocksService = stockService;
		}

		[HttpGet]
		public async Task<object> ListAsync()
		{
			var data = await _stocksService.GetAvailableStocks();

			var groups = data.FilteredList.GroupBy(s => s.PriceBucket)
				.Select(g => new {
					bucket = g.Key,
					count = g.Count()
				})
				.OrderBy(a => a.bucket);

			return new {
				list = data.FilteredList,
				groups
			};
		}
		
		[HttpGet("{ticker}")]
		public async Task<object> DetailsAsync(string ticker)
		{
			var data = await _stocksService.GetHistoricalDataAsync(ticker);

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

			var metrics = await _stocksService.GetKeyMetrics(ticker);

			var mostRecent = metrics.Metrics.FirstOrDefault();

			int age = 0;
			if (mostRecent != null)
			{
				age = (int)(DateTime.UtcNow.Subtract(mostRecent.Date).TotalDays / 30);
			}

			var ratings = await _stocksService.GetRatings(ticker);

			var ratingInfo = new {
				rating = ratings.Rating,
				details = ratings.RatingDetails.Select(d => new {
					name = d.Key,
					rating = d.Value
				})
			};

			return new
			{
				ticker,
				price,
				largestGain,
				largestLoss,
				priceLabels,
				priceValues,
				volumeValues,
				age,
				bookValue = mostRecent?.BookValuePerShare,
				peValue = mostRecent?.PERatio,
				bookValues = metrics.Metrics.Select(m => m.BookValuePerShare),
				peValues = metrics.Metrics.Select(m => m.PERatio),
				ratings = ratingInfo
			};
		}
	}
}
