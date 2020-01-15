using System;
using System.Net.Http;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using Newtonsoft.Json;

namespace financialmodelingclient
{
    public class StocksService : IStocksService
	{
		private static HttpClient _client = new HttpClient();
		private static string _endpoint = "https://financialmodelingprep.com/api/v3";

		public async Task<HistoricalResponse> GetHistoricalDataAsync(string ticker)
		{
			var from = DateTime.UtcNow.AddMonths(-16).ToString("yyyy-MM-dd");
			var to = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

			var url = $"{_endpoint}/historical-price-full/{ticker}?from={from}&to={to}";

			var r = await _client.GetAsync(url);

			r.EnsureSuccessStatusCode();

			var response = await r.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<HistoricalResponse>(response);
		}

		public async Task<CompanyProfile> GetCompanyProfile(string ticker)
		{
			var url = $"{_endpoint}/company/profile/{ticker}";

			var r = await _client.GetAsync(url);

			r.EnsureSuccessStatusCode();

			var response = await r.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<CompanyProfile>(response);
		}

		public async Task<StockRatings> GetRatings(string ticker)
		{
			var url = $"{_endpoint}/company/rating/{ticker}";

			var r = await _client.GetAsync(url);

			r.EnsureSuccessStatusCode();

			var response = await r.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<StockRatings>(response);
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
}