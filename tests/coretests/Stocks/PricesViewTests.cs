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
            Assert.Equal(20, view.SMA.SMA20.Interval);
            Assert.Equal(50, view.SMA.SMA50.Interval);
            Assert.Equal(150, view.SMA.SMA150.Interval);
            Assert.Equal(200, view.SMA.SMA200.Interval);
            Assert.Equal(504, view.SMA.SMA20.Values.Length);
            Assert.Equal(504, view.SMA.SMA50.Values.Length);
            Assert.Equal(504, view.SMA.SMA150.Values.Length);
            Assert.Equal(504, view.SMA.SMA200.Values.Length);
            Assert.Equal(493.5m, view.SMA.SMA20.LastValue);
            Assert.Equal(478.5m, view.SMA.SMA50.LastValue);
            Assert.Equal(428.5m, view.SMA.SMA150.LastValue);
            Assert.Equal(403.5m, view.SMA.SMA200.LastValue);
        }    
    }
}