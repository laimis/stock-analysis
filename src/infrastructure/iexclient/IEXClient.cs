using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace iexclient
{
    public class IEXClient
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

        public Task<OptionDetail[]> GetOptionDetails(string ticker, string optionDate)
        {
            var url = $"{_endpoint}/stock/{ticker}/options/{optionDate}?token={_token}";

            return Get<OptionDetail[]>(url);
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