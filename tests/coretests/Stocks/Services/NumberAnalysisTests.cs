using core.Stocks.Services.Analysis;
using Xunit;

namespace coretests.Stocks.Services
{
    public class NumberAnalysis_PercentChangesTests
    {
        private DistributionStatistics _result;

        public NumberAnalysis_PercentChangesTests()
        {
            _result = NumberAnalysis.PercentChanges(new decimal[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void MeanIsCorrect() => Assert.Equal(0.52m, _result.mean);

        [Fact]
        public void StdDevIsCorrect() => Assert.Equal(0.34m, _result.stdDev);

        [Fact]
        public void MinIsCorrect() => Assert.Equal(0.25m, _result.min);

        [Fact]
        public void MaxIsCorrect() => Assert.Equal(1m, _result.max);

        [Fact]
        public void MedianIsCorrect() => Assert.Equal(0.50m, _result.median);

        [Fact]
        public void SkewnessIsCorrect() => Assert.Equal(0.54m, _result.skewness);

        [Fact]
        public void KurtosisIsCorrect() => Assert.Equal(-1.88m, _result.kurtosis);

        [Fact]
        public void BucketsAreCorrect()
        {
            Assert.Equal(21, _result.buckets.Length);
            Assert.Equal(-10, _result.buckets[0].percentChange);
            Assert.Equal(0, _result.buckets[0].frequency);
            Assert.Equal(-9, _result.buckets[1].percentChange);
            Assert.Equal(0, _result.buckets[1].frequency);
            Assert.Equal(10, _result.buckets[^1].percentChange);
            Assert.Equal(4, _result.buckets[^1].frequency);
        }

        [Fact]
        public void CountIsCorrect() => Assert.Equal(4, _result.count);
    }
}