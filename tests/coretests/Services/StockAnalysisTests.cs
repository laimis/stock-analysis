using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services;
using Xunit;

namespace coretests.Services
{
    public class StockAnalysisTests
    {
        private List<AnalysisOutcome> _outcomes;

        public StockAnalysisTests()
        {
            var start = new DateTime(2020, 1, 1, 1, 1, 1);
            var historcalPrices = Enumerable.Range(1, 31)
                .Select(n => new HistoricalPrice { Close = n, Date = start.AddDays(n).ToString()})
                .ToArray();

            _outcomes = StockAnalysis.Run(10, historcalPrices);
        }

        [Fact]
        public void OutcomesMatch() => Assert.Equal(16, _outcomes.Count);
    }
}