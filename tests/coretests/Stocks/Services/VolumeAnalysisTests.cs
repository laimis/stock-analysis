using System.Collections.Generic;
using core.fs.Services;
using core.fs.Services.Analysis;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks.Services;

public class VolumeAnalysisTests
{
    private readonly List<AnalysisOutcome> _outcomes = MultipleBarPriceAnalysis.VolumeAnalysis.generate(
        TestDataGenerator.IncreasingPriceBars(numOfBars: 300));
    
    [Fact]
    public void VolumeAnalysis_Adds_AllOutcomes() =>
        Assert.Single(_outcomes);
    
    private void OutcomeExistsAndValueMatches(string key, decimal value) =>
        Assert.Contains(_outcomes, o => o.Key == key && o.Value == value);
    
    [Fact]
    public void AverageVolume_PresentAndValid() =>
        OutcomeExistsAndValueMatches(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.AverageVolume, 269);
}