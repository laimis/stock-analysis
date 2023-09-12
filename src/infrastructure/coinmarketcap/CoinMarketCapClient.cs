using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using core;
using core.Shared;
using core.Shared.Adapters.Cryptos;
using Microsoft.Extensions.Logging;

namespace coinmarketcap
{
    public class CoinMarketCapClient : ICryptoService
    {
        private static HttpClient _httpClient = new HttpClient();
        private ILogger<CoinMarketCapClient>? _logger;

        public CoinMarketCapClient(ILogger<CoinMarketCapClient>? logger, string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", accessToken);
            _logger = logger;
        }

        public async Task<Listings> Get()
        {
            var url = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest";

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStreamAsync();

            var value = await JsonSerializer.DeserializeAsync<Listings>(content);
            if (value == null)
            {
                throw new System.Exception("Could not deserialize response: " + content);
            }

            return value;
        }

        public async Task<Price?> Get(string token)
        {
            var prices = await Get();

            if (prices.TryGet(token, out var price))
            {
                return price;
            }

            _logger?.LogError("Did not find price for " + token);

            return null;
        }

        public async Task<Dictionary<string, Price>> Get(IEnumerable<string> tokens)
        {
            var prices = await Get();

            var result = new Dictionary<string, Price>();

            foreach (var token in tokens)
            {
                if (prices.TryGet(token, out var price))
                {
                    result.Add(token, price!.Value);
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
