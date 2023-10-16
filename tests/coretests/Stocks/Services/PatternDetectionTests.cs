using System.Linq;
using core.fs.Services;
using core.fs.Shared.Adapters.Stocks;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks.Services
{
    public class PatternDetectionTests
    {
        [Fact]
        public void Generate_WithOnlyOneBarReturnsNothing()
        {
            var bars = new PriceBar[1];
            var patterns = PatternDetection.generate(bars);
            Assert.Empty(patterns);
        }

        [Fact]
        public void Generate_WithENPH_FindsUpsideReversal()
        {
            var bars = TestDataGenerator.PriceBars("ENPH");
            var patterns = PatternDetection.generate(bars);
            Assert.Single(patterns);

            var pattern = patterns.First();
            Assert.Equal(PatternDetection.upsideReversalName, pattern.name);
            Assert.Contains(", volume x0.9", pattern.description);
        }

        [Fact]
        public void Generate_WithENPH_FindsXVolume()
        {
            var bars = TestDataGenerator.PriceBars("ENPH");

            // find the bar with the highest volume
            var highestVolume = bars.Max(x => x.Volume);
            var barIndex = bars.ToList().FindIndex(x => x.Volume == highestVolume);

            // generate new array that contains bars from 0 to the highest bar index (inclusive)
            // and run pattern finder, it should find two: highest volume and x volume
            var barsToTest = bars.Take(barIndex + 1).ToArray();

            var patterns = PatternDetection.generate(barsToTest);
            Assert.Equal(2, patterns.Count());
            Assert.Equal(PatternDetection.highest1YearVolumeName, patterns.Last().name);
            Assert.Equal(PatternDetection.highVolumeName, patterns.First().name);
        }

        [Fact]
        public void Generate_WithSmallInput_ReturnsNothing()
        {
            var bars = TestDataGenerator.IncreasingPriceBars();
            var patterns = PatternDetection.generate(bars);
            Assert.Empty(patterns);
        }

        [Fact]
        public void HighestVolume_OnLatestBarIsDetected()
        {
            var bars = TestDataGenerator.IncreasingPriceBars(numOfBars: 100);
            
            bars = AppendHighVolumeBar(bars);

            var patterns = PatternDetection.generate(bars);
            Assert.Single(patterns);
            Assert.Equal(PatternDetection.highVolumeName, patterns.First().name);
        }

        private static PriceBar[] AppendHighVolumeBar(PriceBar[] bars)
        {
            // append a bar with 10x the volume of the last bar
            var lastBar = bars[^1];
            var newBar = new PriceBar(lastBar.Date.AddDays(1), lastBar.Open, lastBar.High, lastBar.Low, lastBar.Close, lastBar.Volume * 10);
            bars = bars.Append(newBar).ToArray();
            return bars;
        }

        [Fact]
        public void HighestVolume_OnSmallAmountOfBars_Ignored()
        {
            var bars = TestDataGenerator.IncreasingPriceBars(numOfBars: 10);

            bars = AppendHighVolumeBar(bars);

            var patterns = PatternDetection.generate(bars);

            Assert.Empty(patterns);
        }

        [Fact]
        public void Generate_WithEmptyBars_DoesNotBlowUp()
        {
            var bars = new PriceBar[0];
            var patterns = PatternDetection.generate(bars);
            Assert.Empty(patterns);
        }

        [Fact]
        public void Generate_WithGaps_FindsGapPattern()
        {
            var bars = TestDataGenerator.IncreasingPriceBars(numOfBars: 10);

            // append a bar with a gap up
            var lastBar = bars[^1];
            var newBar = new PriceBar(lastBar.Date.AddDays(1), lastBar.Open * 1.1m, lastBar.High * 1.1m, lastBar.Close * 1.1m, lastBar.Close * 1.1m, lastBar.Volume);
            bars = bars.Append(newBar).ToArray();

            var patterns = PatternDetection.generate(bars);
            Assert.Single(patterns);

            var pattern = patterns.First();
            Assert.Equal(PatternDetection.gapUpName, pattern.name);
            Assert.Equal("Gap Up 10.0%", pattern.description);
        }
    }
}