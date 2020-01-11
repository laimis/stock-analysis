using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using core.Stocks;
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

		public async Task<List<StockActivity>> GetMostActive()
		{
			var r = await GetStockActivity<MostActiveResponse>("actives");

			return r.MostActiveStock;
		}

		public async Task<List<StockActivity>> GetMostGainer()
		{
			var r = await GetStockActivity<MostGainerResponse>("gainers");
			
			return r.MostGainerStock;
		}

		public async Task<List<StockActivity>> GetMostLosers()
		{
			var r = await GetStockActivity<MostLoserResponse>("losers");
			
			return r.MostLoserStock;
		}

		private async Task<T> GetStockActivity<T>(string type)
		{
			var url = $"{_endpoint}/stock/{type}";

			var r = await _client.GetAsync(url);

			r.EnsureSuccessStatusCode();

			var response = await r.Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<T>(response);
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