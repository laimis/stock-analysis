using System.Threading.Tasks;
using testutils;
using Xunit;

namespace coinmarketcaptests
{
    [Trait("Category", "Integration")]
    public class CoinMarketCapClientTests
    {
        [Fact]
        public async Task EndToEnd()
        {
            var creds = CredsHelper.GetCoinMarketCapToken();
            
            var client = new coinmarketcap.CoinMarketCapClient(
                null,
                creds);

            var listings = await client.GetAll();

            Assert.NotNull(listings);

            var price = listings.TryGet("BTC");

            Assert.True(price.Value.Amount > 0);
        }
    }
}
