using System;
using System.Threading.Tasks;
using core.Shared.Adapters.Cryptos;
using testutils;
using Xunit;

namespace coinmarketcaptests
{
    public class CoinMarketCapClientTests
    {
        [Fact]
        public async Task EndToEnd()
        {
            var creds = CredsHelper.GetCoinMarketCapToken();
            
            var client = new coinmarketcap.CoinMarketCapClient(creds);

            core.Shared.Adapters.Cryptos.Listings listings = await client.Get();

            Assert.NotNull(listings);

            listings.TryGet("BTC", out var btc);

            Assert.True(btc.Value.Amount > 0);
        }
    }
}
