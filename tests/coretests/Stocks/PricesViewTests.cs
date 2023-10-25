using core.fs.Shared.Adapters.Stocks;
using core.fs.Stocks;
using Xunit;

namespace coretests.Stocks
{
    public class PricesViewTests
    {
        [Fact]
        public void GivesPrices_SMAs_Are_Correct()
        {
            // generate a set of 504 prices from 0 to 504 increasing by one each day
            var prices = new PriceBar[504];
            var baseDate = new System.DateTimeOffset(2020, 1, 1, 0, 0, 0, System.TimeSpan.Zero);
            for (var i = 0; i < 504; i++)
            {
                prices[i] = new PriceBar(
                    date: baseDate.AddDays(i),
                    i + 1,
                    high: i + 1,
                    low: i + 1,
                    close: i + 1,
                    volume: 1000);
            }

            var view = new PricesView(new PriceBars(prices));

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
            Assert.Equal(493.5m, view.SMA.sma20.LastValue.Value);
            Assert.Equal(478.5m, view.SMA.sma50.LastValue.Value);
            Assert.Equal(428.5m, view.SMA.sma150.LastValue.Value);
            Assert.Equal(403.5m, view.SMA.sma200.LastValue.Value);
        }    
    }
}