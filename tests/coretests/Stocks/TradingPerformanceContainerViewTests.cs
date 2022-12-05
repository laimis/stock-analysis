using core.Stocks.View;
using Xunit;

namespace coretests.Stocks
{
    public class TradingPerformanceContainerViewTests
    {
        private TradingPerformanceContainerView _container;

        public TradingPerformanceContainerViewTests()
        {
            // create a set of closed positions
            _container = new TradingPerformanceContainerView(TradingDataGenerator.GetClosedPositions(), 1);
        }
        
        [Fact]
        public void RecentCorrect() => Assert.Equal(1, _container.Recent.NumberOfTrades);

        [Fact]
        public void OverallCorrect() => Assert.Equal(3, _container.Overall.NumberOfTrades);
        
    }
}