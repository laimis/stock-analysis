using System;
using System.Linq;
using core.fs.Portfolio;
using Xunit;

namespace coretests.Stocks
{
    public class TradingPerformanceContainerViewTests
    {
        private readonly TradingPerformanceContainerView _container =
            new (TradingDataGenerator.GenerateRandomSet(
                DateTimeOffset.Now.AddDays(-100),
                100));
        
        [Fact]
        public void ClosedPositions_Length_Correct() =>
            Assert.True(_container.ClosedPositions.Length > 0);

        [Fact]
        public void RecentPositions_Length_Correct() =>
            Assert.True(_container.RecentClosedPositions.Length > 0);
        
        [Fact]
        public void TradingPerformance_NumberOfTrades_Correct() =>
            Assert.True(_container.Recent.NumberOfTrades < _container.Overall.NumberOfTrades);

        [Fact]
        public void YTDContainer_Profit_DaysAreSequential()
        {
            var dates = _container.TrendsYTD.Single(c => c.Label == "Profits").Data.Select(dp => dp.Label).ToArray();
            
            var parsedAndSorted =
                dates.Select(DateTimeOffset.Parse)
                    .OrderBy(d => d)
                    .Select(d => d.ToString("yyyy-MM-dd"));
            
            Assert.Equal(dates, parsedAndSorted);
        }
        
        [Fact]
        public void YTDContainer_Profit_NoRepeatingDays()
        {
            var dates = _container.TrendsYTD.Single(c => c.Label == "Profits").Data.Select(dp => dp.Label).ToArray();
            
            var distinctDates = dates.Distinct().ToArray();
            
            Assert.Equal(dates.Length, distinctDates.Length);
        }
    }
}