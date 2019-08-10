using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace web.Controllers
{
	[Route("api/[controller]")]
	public class StocksController : Controller
	{
		[HttpGet("{ticker}")]
		public async Task<object> SummaryAsync(string ticker)
		{
			var data = await GetHistoricalDataAsync(ticker);

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
			
			return new
			{
				price,
				largestGain,
				largestLoss,
				priceLabels,
				priceValues,
				volumeValues
			};
		}

		private static HttpClient _client = new HttpClient();

		private async Task<HistoricalResponse> GetHistoricalDataAsync(string ticker)
		{
			var from = DateTime.UtcNow.AddMonths(-12).ToString("yyyy-MM-dd");
			var to = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

			var url = $"https://financialmodelingprep.com/api/v3/historical-price-full/{ticker}?from={from}&to={to}";

			var r = await _client.GetAsync(url);

			r.EnsureSuccessStatusCode();

			var response = await r.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<HistoricalResponse>(response);
		}

		public class StockSummary
		{
			public float Price { get; set; }
		}
	}

	internal class HistoricalResponse
	{
		public HistoricalPriceRecord[] Historical { get; set; }
	}

	public class HistoricalPriceRecord
	{
		public DateTime Date { get; set; }
		public float Open { get; set; }
		public float Close { get; set; }
		public long Volume { get; set; }
		public float ChangePercent { get; set; }
	}
}
