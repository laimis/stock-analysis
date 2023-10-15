using System.Linq;
using core.fs.Services;
using core.fs.Services.Analysis;
using Xunit;

namespace coretests.Stocks.Services
{
    public class NumberAnalysis_PercentChangesTests
    {
        private DistributionStatistics _percentChangeStatistics =
            NumberAnalysis.PercentChanges(false, new decimal[] { 1, 2, 3, 4, 5 });

        [Fact]
        public void MeanIsCorrect() => Assert.Equal(0.52m, _percentChangeStatistics.mean);

        [Fact]
        public void StdDevIsCorrect() => Assert.Equal(0.34m, _percentChangeStatistics.stdDev);

        [Fact]
        public void MinIsCorrect() => Assert.Equal(0.25m, _percentChangeStatistics.min);

        [Fact]
        public void MaxIsCorrect() => Assert.Equal(1m, _percentChangeStatistics.max);

        [Fact]
        public void MedianIsCorrect() => Assert.Equal(0.50m, _percentChangeStatistics.median);

        [Fact]
        public void SkewnessIsCorrect() => Assert.Equal(0.54m, _percentChangeStatistics.skewness);

        [Fact]
        public void KurtosisIsCorrect() => Assert.Equal(-1.88m, _percentChangeStatistics.kurtosis);

        [Fact]
        public void BucketsAreCorrect()
        {
            Assert.Equal(21, _percentChangeStatistics.buckets.Length);
            
            // first bucket should be min
            Assert.Equal(_percentChangeStatistics.min, _percentChangeStatistics.buckets[0].value);
            Assert.Equal(1, _percentChangeStatistics.buckets[0].frequency);
            
            // make sure there are four buckets with values assigned
            Assert.Equal(4, _percentChangeStatistics.buckets.Count(x => x.frequency != 0));
            
            // last bucket should be max
            Assert.True(_percentChangeStatistics.max >= _percentChangeStatistics.buckets[^1].value);
            Assert.Equal(1, _percentChangeStatistics.buckets[^1].frequency);
        }

        [Fact]
        public void CountIsCorrect() => Assert.Equal(4, _percentChangeStatistics.count);
    }
}