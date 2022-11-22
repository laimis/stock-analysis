using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services.Analysis;
using Xunit;

namespace coretests.Stocks.Services
{
    public class MultipleBarPriceAnalysisTests
    {
        private List<AnalysisOutcome> _outcomes;

        public MultipleBarPriceAnalysisTests()
        {
            var rand = new Random();
            var start = new DateTime(2020, 1, 1, 1, 1, 1);
            var historcalPrices = Enumerable.Range(1, 30)
                .Select(
                    n => new PriceBar(date: start.AddDays(n), open: n, high: n, low: n, close: n, volume: rand.Next(1000000))
                ).ToArray();

            _outcomes = MultipleBarPriceAnalysis.Run(10, historcalPrices);
        }

        [Fact]
        public void OutcomesMatch() => Assert.NotEmpty(_outcomes);
    }
}