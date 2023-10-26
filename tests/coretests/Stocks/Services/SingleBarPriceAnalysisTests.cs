using System.Collections.Generic;
using core.fs.Services.Analysis;
using core.Shared;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks.Services
{
    public static class SingleBarPriceAnalysisFunctions
    {
        public static List<AnalysisOutcome> Generate(Ticker ticker) =>
            SingleBarPriceAnalysis.run(TestDataGenerator.PriceBars(ticker));
    }
    
    // NOTE: this tests single bar analysis with a feed of prices that do not change
    // to make sure analysis still runs and does not breakdown with various
    // exceptions related to stddev and other stats being zero
    public class SingleBarPriceAnalysisTests_FeedWithPricesNotChanging
    {
        private readonly List<AnalysisOutcome> _outcomes = SingleBarPriceAnalysisFunctions.Generate(new Ticker("SWCH"));

        [Fact]
        public void OutcomesMatch() => Assert.NotEmpty(_outcomes);
    }

    public class SingleBarPriceAnalysisTests_NewHighWorks
    {
        [Fact]
        public void Run_WithNewHigh_IncludesNewHighOutcome() =>
            Assert.Contains(
                SingleBarPriceAnalysisFunctions.Generate(new Ticker("SHEL")),
                o => o.Key == SingleBarPriceAnalysis.SingleBarOutcomeKeys.NewHigh && o.Value == 1
            );

        [Fact]
        public void Run_WithoutNewHigh_DoesNotIncludeNewHigh() =>
            Assert.Contains(
                SingleBarPriceAnalysisFunctions.Generate(TestDataGenerator.NET),
                o => o.Key == SingleBarPriceAnalysis.SingleBarOutcomeKeys.NewHigh && o.Value == 0
            );
    }

    public class SingleBarPriceAnalysisTests
    {
        private readonly List<AnalysisOutcome> _outcomes = SingleBarPriceAnalysisFunctions.Generate(TestDataGenerator.NET);
        
        [Fact]
        public void OutcomesMatch() => Assert.NotEmpty(_outcomes);
        
        [Fact]
        public void Volume() =>
            Assert.Equal(6933219m, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.Volume).Value);
        
        [Fact]
        public void RelativeVolume() =>
            Assert.Equal(1.34m, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.RelativeVolume).Value);
        
        [Fact]
        public void Open() =>
            Assert.Equal(43.95m, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.Open).Value);
        
        [Fact]
        public void Close() =>
            Assert.Equal(49.14m, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.Close).Value);
        
        [Fact]
        public void ClosingRange() =>
            Assert.Equal(1m, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.ClosingRange).Value);
        
        [Fact]
        public void PercentChange() =>
            Assert.Equal(0.10m, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.PercentChange).Value, 2);
        
        [Fact]
        public void SigmaRatio() =>
            Assert.Equal(1.62m, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.SigmaRatio).Value);
        
        [Fact]
        public void GapPercentage() =>
            Assert.Equal(0m, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.GapPercentage).Value);
        
        [Fact]
        public void NewLow() =>
            Assert.Equal(0, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.NewLow).Value);
        
        [Fact]
        public void NewHigh() =>
            Assert.Equal(0, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.NewHigh).Value);
        
        [Fact]
        public void SMA20Above50Days() =>
            Assert.Equal(47, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.SMA20Above50Days).Value);
        
        [Fact]
        public void PriceAbove20SMA() =>
            Assert.Equal(0.0396m, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.PriceAbove20SMA).Value, 3);
        
        [Fact]
        public void TrueRange() =>
            Assert.Equal(6m, _outcomes.FirstOutcome(SingleBarPriceAnalysis.SingleBarOutcomeKeys.TrueRange).Value, 2);
    }
}