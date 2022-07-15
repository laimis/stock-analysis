using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using core;
using core.Adapters.Options;
using core.Adapters.Stocks;
using core.Shared.Adapters.Stocks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace iexclient
{
    public class IEXClient : IOptionsService, IStocksService2
    {
        private static HttpClient _client = new HttpClient();
        private static string _endpoint = "https://cloud.iexapis.com/stable";
        private ILogger<IEXClient> _logger;
        private string _token;
        private string _tempDir;
        private bool _useCache;

        public IEXClient(string accessToken, ILogger<IEXClient> logger, bool useCache = true)
        {
            _logger = logger;
            _token = accessToken;
            _tempDir = Path.Combine(Path.GetTempPath(), "iexcache");

            _useCache = useCache;

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

            if (!details.IsOk)
            {
                throw new Exception(details.Error.Message);
            }

            return details.Success
                .OrderByDescending(o => o.StrikePrice)
                .ThenBy(o => o.Side);
        }

        public async Task<StockServiceResponse<List<SearchResult>>> Search(string fragment, int maxResults)
        {
            var url = MakeUrl($"search/{fragment}");

            var response = await GetCachedResponse<List<SearchResult>>(url, CacheKeyDaily(fragment));

            return response.IsOk switch {
                true => 
                    new StockServiceResponse<List<SearchResult>>(
                        response.Success.Where(r => r.IsSupportedType).Take(5).ToList()
                    ),
                false => throw new Exception(response.Error.Message)
            };
        }

        public Task<StockServiceResponse<CompanyProfile>> GetCompanyProfile(string ticker) =>
            GetCachedResponse<CompanyProfile>(
                MakeUrl($"stock/{ticker}/company"),
                CacheKeyMonthly(ticker)
            );

        public Task<StockServiceResponse<Quote>> Quote(string ticker) =>
            GetCachedResponse<Quote>(
                MakeUrl($"stock/{ticker}/quote"),
                CacheKeyMinute(ticker + "quote")
            );

        public Task<StockServiceResponse<StockAdvancedStats>> GetAdvancedStats(string ticker) =>
            GetCachedResponse<StockAdvancedStats>(
                MakeUrl($"stock/{ticker}/advanced-stats"),
                CacheKeyDaily(ticker + "advanced")
            );

        public async Task<StockServiceResponse<Price>> GetPrice(string ticker)
        {
            var url = MakeUrl($"stock/{ticker}/price");

            var r = await _client.GetAsync(url);

            if (r.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new StockServiceResponse<Price>(new Price());
            }

            var response = await r.Content.ReadAsStringAsync();

            return new StockServiceResponse<Price>(new Price(JsonConvert.DeserializeObject<decimal>(response)));
        }

        public async Task<StockServiceResponse<Dictionary<string, BatchStockPrice>>> GetPrices(IEnumerable<string> tickers)
        {
            var symbols = string.Join(",", tickers);
            if (symbols == "")
            {
                return new StockServiceResponse<Dictionary<string, BatchStockPrice>>(
                    new Dictionary<string, BatchStockPrice>()
                );
            }
            
            var url = MakeUrl($"stock/market/batch");

            url += $"&symbols={symbols}&types=price";

            var r = await _client.GetAsync(url);

            var response = await r.Content.ReadAsStringAsync();

            if (!r.IsSuccessStatusCode)
            {
                _logger?.LogError($"Failed to get stocks with url {url}: " + response);
                return new StockServiceResponse<Dictionary<string, BatchStockPrice>>(
                    new Dictionary<string, BatchStockPrice>()
                );
            }

            return new StockServiceResponse<Dictionary<string, BatchStockPrice>>(
                JsonConvert.DeserializeObject<Dictionary<string, BatchStockPrice>>(response)
            );
        }

        public Task<StockServiceResponse<HistoricalPrice[]>> GetHistoricalPrices(string ticker, string interval) =>
            GetCachedResponse<HistoricalPrice[]>(
                MakeUrl($"stock/{ticker}/chart/{interval}") + $"&chartCloseOnly=true",
                CacheKeyDaily(ticker + interval)
            );

        private string MakeUrl(string function) =>  $"{_endpoint}/{function}?token={_token}";

        private async Task<StockServiceResponse<T>> GetCachedResponse<T>(string url, string key)
        {
            var file = Path.Combine(_tempDir, key);

            string contents = null;
            if (File.Exists(file) && _useCache)
            {
                contents = File.ReadAllText(file);
            }
            else
            {
                var r = await _client.GetAsync(url);

                contents = await r.Content.ReadAsStringAsync();
                    
                if (r.IsSuccessStatusCode)
                {
                    File.WriteAllText(file, contents);
                }
                else
                {
                    return new StockServiceResponse<T>(
                        new ServiceError(contents)
                    );
                }
            }

            return new StockServiceResponse<T>(
                JsonConvert.DeserializeObject<T>(contents)
            );
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