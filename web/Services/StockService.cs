using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace web.Services
{
	public class StockService
	{
		private static HttpClient _client = new HttpClient();
		private static string _endpoint = "https://financialmodelingprep.com/api/v3";

		public async Task<HistoricalResponse> GetHistoricalDataAsync(string ticker)
		{
			var from = DateTime.UtcNow.AddMonths(-12).ToString("yyyy-MM-dd");
			var to = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

			var url = $"{_endpoint}/historical-price-full/{ticker}?from={from}&to={to}";

			var r = await _client.GetAsync(url);

			r.EnsureSuccessStatusCode();

			var response = await r.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<HistoricalResponse>(response);
		}

		public async Task<MetricsResponse> GetKeyMetrics(string ticker)
		{
			var url = $"{_endpoint}/company-key-metrics/{ticker}?period=quarter";

			var r = await _client.GetAsync(url);

			r.EnsureSuccessStatusCode();

			var response = await r.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<MetricsResponse>(response);
		}
	}

	public class StockSummary
	{
		public float Price { get; set; }
	}

	public class MetricsResponse
	{
		public CompanyKeyMetric[] Metrics { get; set; }
	}

	public class HistoricalResponse
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

	public class CompanyKeyMetric
	{
		public DateTime Date { get; set; }
		
		[JsonProperty("Revenue per Share")]
		public float RevenuePerShare { get; set; }

		[JsonProperty("Book Value per Share")]
		public float BookValuePerShare { get; set; }

		[JsonProperty("PE ratio")]
		public float PERatio { get; set; }
	}
}