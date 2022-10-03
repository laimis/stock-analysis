using core.Shared.Adapters.Stocks;
using core.Stocks.View;
using Xunit;

namespace coretests.Stocks
{
    public class PricesViewTests
    {
        [Fact]
        public void GivesPrices_SMAs_Are_Correct()
        {
            // generate a set of 504 prices from o to 504 increasing by one each day
            var prices = new HistoricalPrice[504];
            for (var i = 0; i < 504; i++)
            {
                prices[i] = new HistoricalPrice
                {
                    Date = $"2020-01-{i + 1}",
                    Close = i + 1,
                    Volume = i + 1
                };
            }

            var view = new PricesView(prices);

            Assert.Equal(504, view.Prices.Length);
            Assert.Equal(4, view.SMA.Length);
            Assert.Equal(20, view.SMA.All[0].Interval);
            Assert.Equal(50, view.SMA.All[1].Interval);
            Assert.Equal(150, view.SMA.All[2].Interval);
            Assert.Equal(200, view.SMA.All[3].Interval);
            Assert.Equal(504, view.SMA.All[0].Values.Length);
            Assert.Equal(504, view.SMA.All[1].Values.Length);
            Assert.Equal(504, view.SMA.All[2].Values.Length);
            Assert.Equal(504, view.SMA.All[3].Values.Length);
            Assert.Equal(493.5m, view.SMA.All[0].LastValue);
            Assert.Equal(478.5m, view.SMA.All[1].LastValue);
            Assert.Equal(428.5m, view.SMA.All[2].LastValue);
            Assert.Equal(403.5m, view.SMA.All[3].LastValue);
        }    
    }
}