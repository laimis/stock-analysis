using core.fs.Shared.Adapters.Stocks;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks;

public class PriceBarTests
{
    private readonly PriceBar _bar = TestDataGenerator.PriceBars(TestDataGenerator.NET).First;
    
    [Fact]
    public void ClosingRange_Works() => Assert.Equal(0.75m, _bar.ClosingRange(), 2);
    
    [Fact]
    public void PercentDifferenceFromHigh_Works() => Assert.Equal(0.02m, _bar.PercentDifferenceFromHigh(_bar.Close), 2);
    
    [Fact]
    public void PercentDifferenceFromLow_Works() => Assert.Equal(-0.07m, _bar.PercentDifferenceFromLow(_bar.Close), 2);
    
    [Fact]
    public void DateStr_Works() => Assert.Equal("2020-11-30", _bar.DateStr);
}

public class PriceBarsTests
{
    private readonly PriceBars _bars = TestDataGenerator.PriceBars(TestDataGenerator.NET);
    
    [Fact]
    public void Length_Correct() => Assert.Equal(505, _bars.Length);

    [Fact]
    public void AllButLast_Correct()
    {
        Assert.Equal(504, _bars.AllButLast.Length);
        Assert.Equal("2020-11-30", _bars.AllButLast.First.DateStr);
        Assert.Equal("2022-11-29", _bars.AllButLast.Last.DateStr);
    }
    
    [Fact]
    public void Last_Correct() => Assert.Equal("2022-11-30", _bars.Last.DateStr);

    [Fact]
    public void LatestOrAll_WithSmallerRange_Correct()
    {
        var subset = _bars.LatestOrAll(numberOfBars: 20);
        
        Assert.Equal(20, subset.Length);
        Assert.Equal("2022-11-30", subset.Last.DateStr);
        Assert.Equal("2022-11-02", subset.First.DateStr);
    }

    [Fact]
    public void ClosingPrices_Correct()
    {
        Assert.Equal(505, _bars.ClosingPrices().Length);
        Assert.Equal(75.08m, _bars.ClosingPrices()[0]);
        Assert.Equal(49.14m, _bars.ClosingPrices()[504]);
    }

    [Fact]
    public void Volumes_Correct()
    {
        Assert.Equal(505, _bars.Volumes().Length);
        Assert.Equal(17157618m, _bars.Volumes()[0]);
        Assert.Equal(6933219m, _bars.Volumes()[504]);
    }
}