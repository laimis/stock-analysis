using System.Collections.Generic;
using System.Linq;
using core.Stocks.Services.Analysis;
using coretests.TestData;
using Xunit;

namespace coretests.Stocks.Services
{

    public static class AnalysisOutcomeExtensions
    {
        public static AnalysisOutcome FirstOutcome(this List<AnalysisOutcome> outcomes, string key) => outcomes.First(x => x.key == key);
    }

    public class MultipleBarPriceAnalysisTests
    {
        private List<AnalysisOutcome> _outcomes;

        public MultipleBarPriceAnalysisTests()
        {
            var bars = TestDataGenerator.PriceBars();

            _outcomes = MultipleBarPriceAnalysis.Run(bars[^1].Close, bars);
        }

        [Fact]
        public void OutcomesMatch() => Assert.NotEmpty(_outcomes);

        [Fact]
        public void PercentAboveLow()
            => Assert.Equal(0.3m, _outcomes.FirstOutcome(MultipleBarOutcomeKeys.PercentAboveLow).value, 2);

        [Fact]
        public void PercentBelowHigh()
            => Assert.Equal(0.77m, _outcomes.FirstOutcome(MultipleBarOutcomeKeys.PercentBelowHigh).value, 2);

        [Fact]
        public void PercentChangeAverage()
            => Assert.Equal(0.00m, _outcomes.FirstOutcome(MultipleBarOutcomeKeys.PercentChangeAverage).value, 2);

        [Fact]
        public void PercentChangeStandardDeviation()
            => Assert.Equal(0.07m, _outcomes.FirstOutcome(MultipleBarOutcomeKeys.PercentChangeStandardDeviation).value, 2);

        [Fact]
        public void LowestPrice()
            => Assert.Equal(37.84m, _outcomes.FirstOutcome(MultipleBarOutcomeKeys.LowestPrice).value, 2);

        [Fact]
        public void HighestPrice()
            => Assert.Equal(217.25m, _outcomes.FirstOutcome(MultipleBarOutcomeKeys.HighestPrice).value, 2);

        [Fact]
        public void LowestPriceDaysAgo()
            => Assert.Equal(24, _outcomes.FirstOutcome(MultipleBarOutcomeKeys.LowestPriceDaysAgo).value, 2);

        [Fact]
        public void HighestPriceDaysAgo()
            => Assert.Equal(380, _outcomes.FirstOutcome(MultipleBarOutcomeKeys.HighestPriceDaysAgo).value, 2);
    }
}