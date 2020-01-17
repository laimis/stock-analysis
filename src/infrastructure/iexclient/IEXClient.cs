using System;
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
    public class IEXClient : IOptionsService, IStocksLists
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

        public async Task<IEnumerable<OptionDetail>> GetOptionDetails(string ticker, string optionDate)
        {
            var url = MakeUrl($"stock/{ticker}/options/{optionDate}");
            
            var key = System.DateTime.UtcNow.ToString("yyyy-MM-dd") + ticker + optionDate + ".json";

            var details = await GetCachedResponse<OptionDetail[]>(url, key);

            return details
                .OrderByDescending(o => o.StrikePrice)
                .ThenBy(o => o.Side);
        }

        private string MakeUrl(string function)
        {
            return $"{_endpoint}/{function}?token={_token}";
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

        public async Task<List<MostActiveEntry>> GetMostActive()
        {
            var url = MakeUrl($"stock/market/list/mostactive");
            
            var key = System.DateTime.UtcNow.ToString("yyyy-MM-dd-hh") + "mostactive.json";

            return await GetCachedResponse<List<MostActiveEntry>>(url, key);
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