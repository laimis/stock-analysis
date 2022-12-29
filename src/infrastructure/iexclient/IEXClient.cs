﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using core;
using core.Adapters.Options;
using core.Adapters.Stocks;
using core.Shared;
using core.Shared.Adapters.Stocks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace iexclient
{
    public class IEXClient : IOptionsService, IStocksService2
    {
        private static HttpClient _client = new HttpClient {
            Timeout = TimeSpan.FromSeconds(5)
        };
        
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

        public async Task<ServiceResponse<Price>> GetPrice(string ticker)
        {
            var url = MakeUrl($"stock/{ticker}/price");

            var r = await _client.GetAsync(url);

            if (r.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ServiceResponse<Price>(new Price());
            }

            var response = await r.Content.ReadAsStringAsync();

            return new ServiceResponse<Price>(new Price(JsonConvert.DeserializeObject<decimal>(response)));
        }

        public Task<ServiceResponse<PriceBar[]>> GetPriceHistory(string ticker, string interval) =>
            GetCachedResponse<PriceBar[]>(
                MakeUrl($"stock/{ticker}/chart/{interval}") + $"&chartCloseOnly=true",
                CacheKeyDaily(ticker + interval)
            );

        private string MakeUrl(string function) =>  $"{_endpoint}/{function}?token={_token}";

        private async Task<ServiceResponse<T>> GetCachedResponse<T>(string url, string key)
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
                    return new ServiceResponse<T>(
                        new ServiceError(contents)
                    );
                }
            }

            return new ServiceResponse<T>(
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