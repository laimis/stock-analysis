using System;
using System.Linq;
using System.Threading.Tasks;
using core.Stocks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
	[Route("api/[controller]")]
    [Authorize]
	public class StocksController : Controller
	{
		private IStocksService _stocksService;

		public StocksController(IStocksService stockService)
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
			var profile = await _stocksService.GetCompanyProfile(ticker);

			var data = await _stocksService.GetHistoricalDataAsync(ticker);

			var price = data.Historical.Last().Close;

			var largestGain = data.Historical.Max(p => p.ChangePercent);
			var largestLoss = data.Historical.Min(p => p.ChangePercent);

			var byMonth = data.Historical.GroupBy(r => r.Date.ToString("yyyy-MM-01"))
				.Select(g => new {
					Date = DateTime.Parse(g.Key),
					Price = g.Average(p => p.Close),
					Volume = g.Average(p => p.Volume),
					Low = g.Min(p => p.Close),
					High = g.Max(p => p.Close)
				});

			var labels = byMonth.Select(a => a.Date.ToString("MMMM"));
			var lowValues = byMonth.Select(a => Math.Round(a.Low, 2));
			var highValues = byMonth.Select(a => Math.Round(a.High, 2));
			
			var ratings = await _stocksService.GetRatings(ticker);
			var ratingInfo = new {
				rating = ratings.Rating?.ToString() ?? "not available",
				details = ratings.RatingDetails?.Select(d => new {
					name = d.Key,
					rating = d.Value
				})
			};

			var priceValues = byMonth.Select(a => Math.Round(a.Price,2));
			var priceChartData = labels.Zip(priceValues, (l, p) => new object[] { l, p});

			var volumeValues = byMonth.Select(a => a.Volume);
			var volumeChartData = labels.Zip(volumeValues, (l, p) => new object[] { l, p});

			var metrics = await _stocksService.GetKeyMetrics(ticker);
			var mostRecent = metrics.Metrics.FirstOrDefault();

			int age = 0;
			if (mostRecent != null)
			{
				age = (int)(DateTime.UtcNow.Subtract(mostRecent.Date).TotalDays / 30);
			}

			var metricDates = metrics.Metrics.Select(m => m.Date.ToString("MM/yy")).Reverse();

			var bookValues = metrics.Metrics.Select(m => m.BookValuePerShare).Reverse();
			var bookChartData = metricDates.Zip(bookValues, (l, p) => new object[] { l, p});

			var peValues = metrics.Metrics.Select(m => m.PERatio).Reverse();
			var peChartData = metricDates.Zip(peValues, (l, p) => new object[] { l, p});

			return new
			{
				ticker,
				price,
				profile = profile.Profile,
				age,
				bookValue = mostRecent?.BookValuePerShare,
				peValue = mostRecent?.PERatio,
				ratings = ratingInfo,
				largestGain,
				largestLoss,
				labels,
				priceChartData,
				volumeChartData,
				bookChartData,
				peChartData
			};
		}
	}
}
