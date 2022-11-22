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
            var prices = new PriceBar[504];
            for (var i = 0; i < 504; i++)
            {
                prices[i] = new PriceBar(
                    date: System.DateTimeOffset.Parse($"2020-01-{i + 1}"),
                    open: i + 1,
                    high: i + 1,
                    low: i + 1,
                    close: i + 1,
                    volume: 1000);
            }

            var view = new PricesView(prices);

            Assert.Equal(504, view.Prices.Length);
            Assert.Equal(4, view.SMA.Length);
            Assert.Equal(20, view.SMA.sma20.Interval);
            Assert.Equal(50, view.SMA.sma50.Interval);
            Assert.Equal(150, view.SMA.sma150.Interval);
            Assert.Equal(200, view.SMA.sma200.Interval);
            Assert.Equal(504, view.SMA.sma20.Values.Length);
            Assert.Equal(504, view.SMA.sma50.Values.Length);
            Assert.Equal(504, view.SMA.sma150.Values.Length);
            Assert.Equal(504, view.SMA.sma200.Values.Length);
            Assert.Equal(493.5m, view.SMA.sma20.LastValue);
            Assert.Equal(478.5m, view.SMA.sma50.LastValue);
            Assert.Equal(428.5m, view.SMA.sma150.LastValue);
            Assert.Equal(403.5m, view.SMA.sma200.LastValue);
        }    
    }
}