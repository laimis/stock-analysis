using System.Linq;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services.Analysis;
using coretests.TestData;
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

        [Fact]
        public void Generate_WithENPH_FindsUpsideReversal()
        {
            var bars = TestDataGenerator.PriceBars("ENPH");
            var patterns = PatternDetection.Generate(bars);
            Assert.Single(patterns);
            Assert.Equal(PatternDetection.UpsideReversalName, patterns.First().name);
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

            var patterns = PatternDetection.Generate(barsToTest);
            Assert.Equal(2, patterns.Count());
            Assert.Equal(PatternDetection.Highest1YearVolumeName, patterns.First().name);
            Assert.Equal(PatternDetection.XVolumeName(), patterns.Last().name);
        }
    }
}