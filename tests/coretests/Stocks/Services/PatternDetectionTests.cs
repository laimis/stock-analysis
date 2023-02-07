using core.Shared.Adapters.Stocks;
using core.Stocks.Services.Analysis;
using Xunit;

namespace coretests.Stocks.Services
{
    public class PatternDetectionTests
    {
        [Fact]
        public void Generate_WithOnlyOneBarReturnsNothing()
        {
            var bars = new PriceBar[1];
            var patterns = PatternDetection.Generate(bars);
            Assert.Empty(patterns);
        }   
    }
}