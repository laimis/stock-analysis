using System.Threading.Tasks;
using coinmarketcap;
using core.fs.Adapters.Cryptos;
using Microsoft.FSharp.Core;
using testutils;
using Xunit;

namespace coinmarketcaptests
{
    [Trait("Category", "Integration")]
    public class CoinMarketCapClientTests
    {
        private static readonly CoinMarketCapClient client = new(
            null,
            CredsHelper.GetCoinMarketCapToken());
        

        private void VerifyBTC(FSharpOption<Datum> btcDataOption)
        {
            Assert.NotNull(btcDataOption);
            
            var btcData = btcDataOption.Value;
            
            Assert.True(btcData.quote.Value.USD.Value.price > 0);
            Assert.Equal(1, btcData.cmc_rank);
            Assert.Equal("BTC", btcData.symbol);
            Assert.Equal("Bitcoin", btcData.name);
        }
        
        [Fact]
        public async Task GetAllWorks()
        {
            var listings = await client.GetAll();

            Assert.NotNull(listings);
            Assert.True(listings.data.Length > 0);

            VerifyBTC(listings.TryGet("BTC"));
        }
        
        [Fact]
        public async Task GetWorks()
        {
            var btcDataOption = await client.Get("BTC");
            
            VerifyBTC(btcDataOption);
        }
        
        [Fact]
        public async Task GetManyWorks()
        {
            var btcDataOption = await client.Get(new [] { "BTC", "ETH" });
            
            VerifyBTC(btcDataOption["BTC"]);
            Assert.NotNull(btcDataOption["ETH"]);
        }
    }
}
