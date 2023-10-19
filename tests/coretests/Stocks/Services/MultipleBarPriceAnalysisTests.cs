using System.Collections.Generic;
using System.Linq;
using core.fs.Services;
using core.fs.Services.Analysis;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks.Services
{

    public static class AnalysisOutcomeExtensions
    {
        public static AnalysisOutcome FirstOutcome(this List<AnalysisOutcome> outcomes, string key) => outcomes.First(x => x.Key == key);
    }

    public class MultipleBarPriceAnalysisTests
    {
        private List<AnalysisOutcome> _outcomes;

        public MultipleBarPriceAnalysisTests()
        {
            var bars = TestDataGenerator.PriceBars();

            _outcomes = MultipleBarPriceAnalysis.MultipleBarPriceAnalysis.Run(bars[^1].Close, bars);
        }

        [Fact]
        public void OutcomesMatch() => Assert.NotEmpty(_outcomes);

        [Fact]
        public void PercentAboveLow()
            => Assert.Equal(0.3m, _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PercentAboveLow).Value, 2);

        [Fact]
        public void PercentBelowHigh()
            => Assert.Equal(0.77m, _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PercentBelowHigh).Value, 2);

        [Fact]
        public void PercentChangeAverage()
            => Assert.Equal(-0.16m, _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PercentChangeAverage).Value, 2);

        [Fact]
        public void PercentChangeStandardDeviation()
            => Assert.Equal(6.62m, _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PercentChangeStandardDeviation).Value, 2);

        [Fact]
        public void LowestPrice()
            => Assert.Equal(37.84m, _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.LowestPrice).Value, 2);
        
        [Fact]
        public void LowestPriceDaysAgo()
            => Assert.Equal(344, _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.LowestPriceDaysAgo).Value);

        [Fact]
        public void HighestPrice()
            => Assert.Equal(217.25m, _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.HighestPrice).Value, 2);

        [Fact]
        public void HighestPriceDaysAgo()
            => Assert.True(
                _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.HighestPriceDaysAgo).Value > _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.LowestPriceDaysAgo).Value
            );
        
        [Fact]
        public void CurrentPrice()
            => Assert.Equal(49.14m, _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.CurrentPrice).Value, 2);
        
        [Fact]
        public void EarliestPrice()
            => Assert.Equal(75.08m, _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.EarliestPrice).Value, 2);
        
        [Fact]
        public void Gain()
            => Assert.Equal(-0.35m, _outcomes.FirstOutcome(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.Gain).Value, 2);
        
    }
}