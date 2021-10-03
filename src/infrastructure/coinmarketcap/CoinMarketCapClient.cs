using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using core.Shared.Adapters.Cryptos;

namespace coinmarketcap
{
    public class CoinMarketCapClient : ICryptoService
    {
        private static HttpClient _httpClient = new HttpClient();

        public CoinMarketCapClient(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", accessToken);
        }

        public async Task<Listings> Get()
        {
            var url = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest";

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStreamAsync();

            var value = await JsonSerializer.DeserializeAsync<Listings>(content);

            return value;
        }
    }
}
