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
            var url = $"{_endpoint}/stock/{ticker}/options?token={_token}";

            return Get<string[]>(url);
        }

        public async Task<IEnumerable<OptionDetail>> GetOptionDetails(string ticker, string optionDate)
        {
            // option details are expensive to call, cache locally once per day per ticker per option date
            var key = System.DateTime.UtcNow.ToString("yyyy-MM-dd") + ticker + optionDate + ".json";

            var file = Path.Combine(_tempDir, key);

            string contents = null;
            if (File.Exists(file))
            {
                contents = File.ReadAllText(file);
            }
            else
            {
                var url = $"{_endpoint}/stock/{ticker}/options/{optionDate}?token={_token}";

                var r = await _client.GetAsync(url);

                r.EnsureSuccessStatusCode();

                contents = await r.Content.ReadAsStringAsync();

                File.WriteAllText(file, contents);
            }

            var details = JsonConvert.DeserializeObject<OptionDetail[]>(contents);

            return details
                .OrderByDescending(o => o.StrikePrice)
                .ThenBy(o => o.Side);
        }

        public async Task<double> GetPrice(string ticker)
        {
            var url = $"{_endpoint}/stock/{ticker}/price?token={_token}";

            return await Get<double>(url);
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