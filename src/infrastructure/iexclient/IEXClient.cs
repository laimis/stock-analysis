using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using core;
using core.Adapters.Options;
using core.Adapters.Stocks;
using Newtonsoft.Json;

namespace iexclient
{
    public class IEXClient : IOptionsService, IStocksService2
    {
        private static HttpClient _client = new HttpClient();
        private static string _endpoint = "https://cloud.iexapis.com/stable";
        private string _token;
        private string _tempDir;

        public IEXClient(string accessToken)
        {
            this._token = accessToken;
            
            this._tempDir = Path.Combine(Path.GetTempPath(), "iexcache");

            if (!Directory.Exists(_tempDir))
            {
                Directory.CreateDirectory(_tempDir);
            }
        }

        public Task<string[]> GetOptions(string ticker)
        {
            return Get<string[]>(MakeUrl($"stock/{ticker}/options"));
        }

        private string CacheKeyDaily(string key) => 
            $"{System.DateTime.UtcNow.ToString("yyyy-MM-dd")}{key}.json";

        private string CacheKeyMonthly(string key) => 
            $"{System.DateTime.UtcNow.ToString("yyyy-MM")}{key}.json";

        private string CacheKeyMinute(string key) => 
            $"{System.DateTime.UtcNow.ToString("yyyy-MM-dd-mm")}{key}.json";

        public async Task<IEnumerable<OptionDetail>> GetOptionDetails(string ticker, string optionDate)
        {
            var url = MakeUrl($"stock/{ticker}/options/{optionDate}");
            
            var details = await GetCachedResponse<OptionDetail[]>(url, CacheKeyDaily(ticker + optionDate));

            return details
                .OrderByDescending(o => o.StrikePrice)
                .ThenBy(o => o.Side);
        }

        public async Task<List<SearchResult>> Search(string fragment)
        {
            var url = MakeUrl($"search/{fragment}");

            return await GetCachedResponse<List<SearchResult>>(url, CacheKeyDaily(fragment));
        }

        public Task<CompanyProfile> GetCompanyProfile(string ticker)
        {
            var url = MakeUrl($"stock/{ticker}/company");

            return GetCachedResponse<CompanyProfile>(url, CacheKeyMonthly(ticker));
        }

        public Task<Quote> Quote(string ticker)
        {
            var url = MakeUrl($"stock/{ticker}/quote");

            return GetCachedResponse<Quote>(url, CacheKeyMinute(ticker + "quote"));
        }

        public Task<StockAdvancedStats> GetAdvancedStats(string ticker)
        {
            var url = MakeUrl($"stock/{ticker}/advanced-stats");

            return GetCachedResponse<StockAdvancedStats>(url, CacheKeyMinute(ticker + "advanced"));
        }

        public async Task<TickerPrice> GetPrice(string ticker)
        {
            var url = MakeUrl($"stock/{ticker}/price");

            var r = await _client.GetAsync(url);

            if (r.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new TickerPrice();
            }

            var response = await r.Content.ReadAsStringAsync();

            return new TickerPrice(JsonConvert.DeserializeObject<double>(response));
        }

        public async Task<Dictionary<string, BatchStockPrice>> GetPrices(List<string> tickers)
        {
            if (tickers.Count == 0)
            {
                return new Dictionary<string, BatchStockPrice>();
            }
            
            var url = MakeUrl($"stock/market/batch");

            url += $"&symbols={string.Join(",", tickers)}&types=price";

            var r = await _client.GetAsync(url);

            var response = await r.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Dictionary<string, BatchStockPrice>>(response);
        }

        private string MakeUrl(string function)
        {
            return $"{_endpoint}/{function}?token={_token}";
        }

        private async Task<T> GetCachedResponse<T>(string url, string key)
        {
            var file = Path.Combine(_tempDir, key);

            string contents = null;
            if (File.Exists(file))
            {
                contents = File.ReadAllText(file);
            }
            else
            {
                var r = await _client.GetAsync(url);

                r.EnsureSuccessStatusCode();

                contents = await r.Content.ReadAsStringAsync();

                File.WriteAllText(file, contents);
            }

            return JsonConvert.DeserializeObject<T>(contents);
        }

        private async Task<T> Get<T>(string url)
        {
            var r = await _client.GetAsync(url);

            r.EnsureSuccessStatusCode();

            var response = await r.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(response);
        }
    }
}