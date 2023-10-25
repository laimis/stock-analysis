using System.Linq;
using core.fs.Services;
using core.fs.Services.Analysis;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks.Services
{
    public class NumberAnalysis_RealPriceBars
    {
        private DistributionStatistics _percentChanges;

        public NumberAnalysis_RealPriceBars()
        {
            var bars = TestDataGenerator.PriceBars(TestDataGenerator.NET);

            _percentChanges = PercentChangeAnalysis.calculateForPriceBars(bars);
        }

        [Fact]
        public void MeanIsCorrect() => Assert.Equal(0.05m, _percentChanges.mean);

        [Fact]
        public void StdDevIsCorrect() => Assert.Equal(5.21m, _percentChanges.stdDev);

        [Fact]
        public void MinIsCorrect() => Assert.Equal(-18.42m, _percentChanges.min);

        [Fact]
        public void MaxIsCorrect() => Assert.Equal(27.06m, _percentChanges.max);

        [Fact]
        public void MedianIsCorrect() => Assert.Equal(0.17m, _percentChanges.median);

        [Fact]
        public void SkewnessIsCorrect() => Assert.Equal(0.36m, _percentChanges.skewness, 2);

        [Fact]
        public void KurtosisIsCorrect() => Assert.Equal(2.81m, _percentChanges.kurtosis, 2);

        [Fact]
        public void BucketsAreCorrect()
        {
            Assert.Equal(21, _percentChanges.buckets.Length);

            // first bucket should be min
            Assert.Equal(-19, _percentChanges.buckets[0].value);
            Assert.Equal(1, _percentChanges.buckets[1].frequency);

            // make sure there are four buckets with values assigned
            Assert.Equal(19, _percentChanges.buckets.Count(x => x.frequency != 0));

            // last bucket should include max
            Assert.True(_percentChanges.max > _percentChanges.buckets[^1].value);
            Assert.Equal(2, _percentChanges.buckets[^1].frequency);
        }
    }
}