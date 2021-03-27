using System.Threading.Tasks;
using iexclient;
using Xunit;
using Xunit.Abstractions;

namespace iexclienttests
{
    public class IEXSpikes
    {
        private ITestOutputHelper _helper;
        private IEXClient _client;

        public IEXSpikes(Xunit.Abstractions.ITestOutputHelper helper)
        {
            _helper = helper;
            _client = new IEXClient("pk_71ba0d8d98ed4d2caac8089588d62973");
        }

        [Fact]
        public async Task Advanced()
        {
            var stats = await _client.GetAdvancedStats("VIOT");

            _helper.WriteLine("Earnings " + stats.NextEarningsDate);
        }

        [Fact]
        public async Task CompanyProfile()
        {
            var stats = await _client.GetCompanyProfile("VIOT");

            _helper.WriteLine("Country " + stats.Country);
        }

        [Fact]
        public async Task Quote()
        {
            var stats = await _client.Quote("VIOT");

            _helper.WriteLine("Country " + stats);
        }
    }
}