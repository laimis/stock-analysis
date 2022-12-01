using System;
using System.IO;
using System.Linq;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services.Analysis;
using Xunit;

namespace coretests.Stocks.Services
{
    public class NumberAnalysis_RealStreamExample
    {
        [Fact]
        public void EndToEndWorks()
        {
            var content = File.ReadAllText("testdata\\pricefeed_NET.txt");

            var numbers = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => PriceBar.Parse(x))
                .ToArray();

            var outcomes = SingleBarAnalysisRunner.Run(numbers);

            Assert.True(outcomes.Count > 0);
        }
    }
}