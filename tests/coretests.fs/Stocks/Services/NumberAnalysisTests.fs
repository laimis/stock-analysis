module coretests.fs.Stocks.Services.NumberAnalysisTests

open Xunit
open core.fs.Services.Analysis
open testutils


module PercentChangesTests =
    let percentChangeStatistics = PercentChangeAnalysis.calculate false [1m; 2m; 3m; 4m; 5m]

    [<Fact>]
    let ``Mean is correct``() = Assert.Equal(0.52m, percentChangeStatistics.mean, 2)

    [<Fact>]
    let ``StdDev is correct``() = Assert.Equal(0.34m, percentChangeStatistics.stdDev, 2)

    [<Fact>]
    let ``Min is correct``() = Assert.Equal(0.25m, percentChangeStatistics.min)

    [<Fact>]
    let ``Max is correct``() = Assert.Equal(1m, percentChangeStatistics.max)

    [<Fact>]
    let ``Median is correct``() = Assert.Equal(0.5m, percentChangeStatistics.median, 2)

    [<Fact>]
    let ``Skewness is correct``() = Assert.Equal(1.47m, percentChangeStatistics.skewness, 2)

    [<Fact>]
    let ``Kurtosis is correct``() = Assert.Equal(2.03m, percentChangeStatistics.kurtosis, 2)

    [<Fact>]
    let ``Buckets are correct``() =
        Assert.Equal(21, percentChangeStatistics.buckets.Length)
        
        // make sure there are four buckets with values assigned
        Assert.Equal(4, percentChangeStatistics.buckets |> Array.filter (fun x -> x.frequency <> 0) |> Array.length)
        
        // last bucket should be max
        let lastBucketValue = percentChangeStatistics.buckets |> Array.last |> _.value
        let lastBucketFrequency = percentChangeStatistics.buckets |> Array.last |> _.frequency
        
        Assert.True(percentChangeStatistics.max >= lastBucketValue)
        Assert.Equal(1, lastBucketFrequency)

    [<Fact>]
    let ``Count is correct``() = Assert.Equal(4m, percentChangeStatistics.count)
    
module RealBarsTests =
    
    let percentChanges = TestDataGenerator.PriceBars(TestDataGenerator.NET) |> PercentChangeAnalysis.calculateForPriceBars true

    [<Fact>]
    let ``Mean is correct``() = Assert.Equal(0.05m, percentChanges.mean, 2)

    [<Fact>]
    let ``StdDev is correct``() = Assert.Equal(5.21m, percentChanges.stdDev, 2)

    [<Fact>]
    let ``Min is correct``() = Assert.Equal(-18.42m, percentChanges.min)

    [<Fact>]
    let ``Max is correct``() = Assert.Equal(27.06m, percentChanges.max)

    [<Fact>]
    let ``Median is correct``() = Assert.Equal(0.17m, percentChanges.median)

    [<Fact>]
    let ``Skewness is correct``() = Assert.Equal(0.36m, percentChanges.skewness, 2)

    [<Fact>]
    let ``Kurtosis is correct``() = Assert.Equal(2.89m, percentChanges.kurtosis, 2)

    [<Fact>]
    let ``Buckets are correct``() =
        Assert.Equal(21, percentChanges.buckets.Length)
        
        Assert.Equal(18, percentChanges.buckets |> Array.filter (fun x -> x.frequency <> 0) |> Array.length)
        
        // last bucket should include max
        let lastBucketValue = percentChanges.buckets |> Array.last |> _.value
        
        Assert.True(percentChanges.max > lastBucketValue)
        Assert.Equal(1, percentChanges.buckets |> Array.last |> _.frequency)
