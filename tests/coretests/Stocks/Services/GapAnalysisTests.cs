using System.Collections.Generic;
using System.Linq;
using core.fs.Services;
using core.fs.Services.Analysis;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks.Services;

public class GapAnalysisTests
{
    private readonly List<GapAnalysis.Gap> _gaps = GapAnalysis.detectGaps(
        TestDataGenerator.PriceBars(TestDataGenerator.NET),
        SingleBarPriceAnalysis.SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis
    );
    
    [Fact]
    public void NumberOfGapsDetected_Matches() => Assert.Equal(11, _gaps.Count);
    
    [Fact]
    public void Gap_PercentChange_Correct() => Assert.Equal(0.052m, _gaps[0].PercentChange, 3);
    
    [Fact]
    public void Gap_RelativeVolume_Correct() => Assert.Equal(0.94m, _gaps[0].RelativeVolume);
    
    [Fact]
    public void Gap_GapType_Correct() => Assert.Equal(GapAnalysis.GapType.Up, _gaps[0].Type);
    
    [Fact]
    public void ClosedQuickly_Matches() => Assert.Equal(7, _gaps.Count(g => g.ClosedQuickly));
    
    [Fact]
    public void Open_Matches() => Assert.Equal(4, _gaps.Count(g => g.Open));
}