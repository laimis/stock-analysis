using System.Collections.Generic;
using System.Linq;
using core.fs.Services;
using core.fs.Services.Analysis;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks.Services;

public class SMAAnalysisTests
{
    private readonly List<AnalysisOutcome> _outcomes = MultipleBarPriceAnalysis.SMAAnalysis.generate(
        TestDataGenerator.IncreasingPriceBars(numOfBars: 260)
    );

    [Fact]
    public void SMAAnalysis_Adds_AllOutcomes() => Assert.Equal(6, _outcomes.Count);

    private void OutcomeExistsAndValueMatches(string key, decimal value)
    {
        Assert.Contains(_outcomes, o => o.Key == key);
        
        Assert.Equal(
            value,
            _outcomes.Single(o => o.Key == key).Value
        );
    }
        
    
    [Fact]
    public void SMA20_Present_And_Valid() => OutcomeExistsAndValueMatches(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA(20), 248.5m);
    
    [Fact]
    public void SMA50_Present_And_Valid() => OutcomeExistsAndValueMatches(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA(50), 233.5m);
    
    [Fact]
    public void SMA150_Present_And_Valid() => OutcomeExistsAndValueMatches(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA(150), 183.5m);
    
    [Fact]
    public void SMA200_Present_And_Valid() => OutcomeExistsAndValueMatches(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA(200), 158.5m);
    
    [Fact]
    public void SMA20_Above_SMA50() => OutcomeExistsAndValueMatches(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA20Above50Days, 210);
    
    [Fact]
    public void Price_Above_20SMA() => OutcomeExistsAndValueMatches(MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PriceAbove20SMADays, 240);

    [Fact]
    public void SMA20_Above_SMA50_Positive() =>
        Assert.Equal(
            OutcomeType.Positive,
            _outcomes.Single(o => o.Key == MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA20Above50Days).OutcomeType
        );
}