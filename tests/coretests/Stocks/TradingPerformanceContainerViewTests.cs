using core.fs.Portfolio;
using Xunit;

namespace coretests.Stocks
{
    public class TradingPerformanceContainerViewTests
    {
        private readonly TradingPerformanceContainerView _container =
            new (TradingDataGenerator.GetClosedPositions(), 1);

        // create a set of closed positions

        [Fact]
        public void RecentCorrect() => Assert.Equal(1, _container.Recent.NumberOfTrades);

        [Fact]
        public void OverallCorrect() => Assert.Equal(3, _container.Overall.NumberOfTrades);
        
    }
}