using System.Collections.Generic;
using core.Stocks.Services.Analysis;
using coretests.TestData;
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

            _outcomes = SingleBarAnalysisRunner.Run(bars[^1], bars[..^1]);
        }

        [Fact]
        public void OutcomesMatch() => Assert.NotEmpty(_outcomes);
    }
}