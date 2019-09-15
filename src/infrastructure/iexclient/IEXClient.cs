using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using core.Options;
using Newtonsoft.Json;

namespace iexclient
{
    public class IEXClient : IOptionsService
    {
        private static HttpClient _client = new HttpClient();
        private static string _endpoint = "https://cloud.iexapis.com/stable";
        private string _token;

        public IEXClient(string accessToken)
        {
            this._token = accessToken;
        }

        public Task<string[]> GetOptions(string ticker)
        {
            var url = $"{_endpoint}/stock/{ticker}/options?token={_token}";

            return Get<string[]>(url);
        }

        public async Task<IEnumerable<OptionDetail>> GetOptionDetails(string ticker, string optionDate)
        {
            var url = $"{_endpoint}/stock/{ticker}/options/{optionDate}?token={_token}";

            var details = await Get<OptionDetail[]>(url);

            return details
                .OrderByDescending(o => o.StrikePrice)
                .ThenBy(o => o.IsPut);
        }

        private async Task<T> Get<T>(string url)
        {
            var r = await _client.GetAsync(url);

            r.EnsureSuccessStatusCode();

            var response = await r.Content.ReadAsStringAsync();

            File.WriteAllText(@"c:\temp\spread.txt", response);

            return JsonConvert.DeserializeObject<T>(response);
        }
    }
}