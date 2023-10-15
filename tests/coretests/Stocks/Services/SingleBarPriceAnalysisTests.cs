using System.Collections.Generic;
using core.fs.Services.Analysis;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks.Services
{
    // NOTE: this tests single bar analysis with a feed of prices that do not change
    // to make sure analysis still runs and does not breakdown with various
    // exceptions related to stddev and other stats being zero
    public class SingleBarPriceAnalysisTests_FeedWithPricesNotChanging
    {
        private List<AnalysisOutcome> _outcomes;

        public SingleBarPriceAnalysisTests_FeedWithPricesNotChanging()
        {
            var bars = TestDataGenerator.PriceBars("SWCH");

            _outcomes = SingleBarPriceAnalysis.run(bars);
        }

        [Fact]
        public void OutcomesMatch() => Assert.NotEmpty(_outcomes);
    }

    public class SingleBarPriceAnalysisTests
    {
        [Fact]
        public void Run_WithNewHigh_IncludesNewHighOutcome()
        {
            var bars = TestDataGenerator.PriceBars("SHEL");

            var outcomes = SingleBarPriceAnalysis.run(bars);

            Assert.Contains(outcomes, o => o.Key == SingleBarPriceAnalysis.SingleBarOutcomeKeys.NewHigh && o.Value == 1);
        }

        [Fact]
        public void Run_WithoutNewHigh_DoesNotIncludeNewHigh()
        {
            var bars = TestDataGenerator.PriceBars("NET");

            var outcomes = SingleBarPriceAnalysis.run(bars);

            Assert.Contains(outcomes, o => o.Key == SingleBarPriceAnalysis.SingleBarOutcomeKeys.NewHigh && o.Value == 0);
        }
    }
}