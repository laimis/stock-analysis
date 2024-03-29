﻿using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using core.fs.Adapters.Cryptos;
using core.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;

namespace coinmarketcap
{
    public class CoinMarketCapClient : ICryptoService
    {
        private static readonly HttpClient _httpClient = new();
        private readonly ILogger<CoinMarketCapClient>? _logger;

        public CoinMarketCapClient(ILogger<CoinMarketCapClient>? logger, string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", accessToken);
            _logger = logger;
        }

        public async Task<Listings> GetAll()
        {
            var url = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest";

            var response = await _httpClient.GetAsync(url);

            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode == false)
            {
                throw new System.Exception("Could not get listings: " + content);
            }
            
            var value = JsonSerializer.Deserialize<Listings>(content);
            if (value == null)
            {
                throw new System.Exception("Could not deserialize response: " + content);
            }

            return value;
        }

        public async Task<FSharpOption<Datum>> Get(string token)
        {
            var prices = await GetAll();

            return prices.TryGet(token);
        }

        public async Task<Dictionary<string, Datum>> Get(IEnumerable<string> tokens)
        {
            var prices = await GetAll();

            var result = new Dictionary<string, Datum>();

            foreach (var token in tokens)
            {
                var price = prices.TryGet(token);
                if (price != null)
                {
                    result.Add(token, price.Value);
                }
                else
                {
                    _logger?.LogError("Did not find price for " + token);
                }
            }

            return result;
        }
    }
}
